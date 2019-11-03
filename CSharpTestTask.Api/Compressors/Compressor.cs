using System;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Threading.Tasks.Dataflow;

namespace CSharpTestTask.Api.Compressors
{
    public class Compressor : ICompressor
    {        
        private volatile int _currentBlockNumber = 0;
        private volatile int _blockNumberToWrite = 0;
        private readonly object key = new object();
        private readonly string _inputFileName;
        private readonly string _outputFileName;
        private readonly int _blockSize;
        private long _fileSize;
        private int _numberOfBlocks;

        private int GetNextBlockNumber()
        {
            lock (key)
            {
                return _currentBlockNumber++;
            }
        }
       
        public Compressor(string inputFileName, string outputFileName, int blockSize = 1048576)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _blockSize = blockSize;            
        }
        private byte[] CompressBlock(byte[] data)
        {
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();            
        }
        private void ProcessBlock()
        {
            var (buffer, blockNumber, readedNumberOfBytes) = ReadPartBytes();
            if (readedNumberOfBytes > 0)
            {
                WriteCompressedPartToOutputFile(buffer, blockNumber, readedNumberOfBytes);
            }
            if (readedNumberOfBytes == _blockSize)
            {
                ProcessBlock();
            }
        }
        private void WriteCompressedPartToOutputFile(byte[] bytes, int blocktNumber, int readedNumberOfBytes)
        {

            SpinWait.SpinUntil(() => blocktNumber == _blockNumberToWrite);
            var compressedBytes = CompressBlock(bytes[0..readedNumberOfBytes]);

            using var fileStream = new FileStream(_outputFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            using var writer = new BinaryWriter(fileStream);

            var headerPosition = 2 * sizeof(int) + sizeof(long) + blocktNumber * sizeof(int);

            writer.Seek(headerPosition, SeekOrigin.Begin);
            writer.Write(compressedBytes.Length);

            writer.Seek(0, SeekOrigin.End);
            writer.Write(compressedBytes, 0, compressedBytes.Length);
          
            _blockNumberToWrite++;
        }

        private (byte[] buffer, int blocktNumber, int readedNumberOfBytes) ReadPartBytes()
        {
            var buffer = new byte[_blockSize];
            var blockNumber = GetNextBlockNumber();
            var offset = blockNumber * _blockSize;

            using var fileStream = new FileStream(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            fileStream.Seek(offset, SeekOrigin.Begin);
          
            var readedNumberOfBytes = fileStream.Read(buffer, 0, _blockSize);           

            return (buffer, blockNumber, readedNumberOfBytes);
        }

        private void CreateOutputFileWithMetaData()
        {          
            long headerSize = 2 * sizeof(int) + sizeof(long) + _numberOfBlocks * sizeof(int);

            using var fileStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(headerSize);

            using var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Seek(0, SeekOrigin.Begin);

            binaryWriter.Write(_numberOfBlocks);
            binaryWriter.Write(_blockSize);
            binaryWriter.Write(_fileSize);
        }
        public (string message, bool success) Compress()       
        {
            _currentBlockNumber = 0;
            _blockNumberToWrite = 0;
            _fileSize = new FileInfo(_inputFileName).Length;
            _numberOfBlocks =(int)(_fileSize / _blockSize) + 1;

            CreateOutputFileWithMetaData();          

            var numberOfThreadsNeeded =
                _numberOfBlocks > Environment.ProcessorCount ?
                Environment.ProcessorCount : _numberOfBlocks;

            for (var i = 0; i < numberOfThreadsNeeded; i++)
            {
                var workingThread = new Thread(ProcessBlock);                               
                workingThread.Start();
            }
            return (numberOfThreadsNeeded.ToString(), true);
        }
    }
}
