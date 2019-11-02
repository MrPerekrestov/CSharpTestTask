using System;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Threading.Tasks.Dataflow;

namespace CSharpTestTask.Api
{
    public class Compressor
    {
        private object key = new object();
        private volatile int _currentPartNumber;
        private volatile int _partNumberToWrite = 0;        
        private readonly string _inputFileName;
        private readonly string _outputFileName;
        private readonly int _blockSize;

        private int GetNextPartNumber()
        {
            lock (key)
            {
                return _currentPartNumber++;
            }
        }
        public Compressor(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _blockSize = 1048576;
        }
        public Compressor(string inputFileName, string outputFileName, int blockSize)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
            _blockSize = blockSize;
        }
        private byte[] CompressBlock(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
        private void CreatePart()
        {
            var (buffer, partNumber, readedNumberOfBytes) = ReadPartBytes();
            if (readedNumberOfBytes > 0)
            {
                WriteCompressedPartToOutputFile(buffer, partNumber, readedNumberOfBytes);
            }            
            if (readedNumberOfBytes == _blockSize)
            {
                CreatePart();
            }
        }
        private void WriteCompressedPartToOutputFile(byte[] bytes, int partNumber, int readedNumberOfBytes)
        {
            SpinWait.SpinUntil(() => partNumber == _partNumberToWrite);
            var compressedBytes = CompressBlock(bytes[0..readedNumberOfBytes]);         
           
            using var fileStream = new FileStream(_outputFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            using var writer = new BinaryWriter(fileStream);
            
            var headerPosition = 2 * sizeof(int) + sizeof(long) + partNumber * sizeof(int);
            writer.Seek(headerPosition, SeekOrigin.Begin);
            writer.Write(compressedBytes.Length);

            writer.Seek(0, SeekOrigin.End);
            
            writer.Write(compressedBytes, 0, compressedBytes.Length);
            Console.WriteLine($"{partNumber} was written");
            _partNumberToWrite++;
        }

        private (byte[] buffer, int partNumber, int readedNumberOfBytes) ReadPartBytes()
        {
            var buffer = new byte[_blockSize];
            var partNumber = GetNextPartNumber();
            var offset = partNumber * _blockSize;

            using var fileStream = new FileStream(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);           
            fileStream.Seek(offset, SeekOrigin.Begin);

            Console.WriteLine($"{Thread.CurrentThread.Name}, {partNumber}, {offset}");
            var readedNumberOfBytes = fileStream.Read(buffer, 0, _blockSize);

            Console.WriteLine($"{Thread.CurrentThread.Name} readed {readedNumberOfBytes} bytes");  
            
            return (buffer,partNumber,readedNumberOfBytes);
        }

        public void AssembleBack()
        {
            var regEx = new Regex($"^{_inputFileName}_part\\d+\\.tmp$");
            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());
            var filesToAssemble = allFiles.Where(file => regEx.IsMatch(file.Split("\\").LastOrDefault())).ToList();
            foreach (var file in filesToAssemble)
            {
                var bytes = File.ReadAllBytes(file);
                using (var fileStream = new FileStream($"as_{_inputFileName}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    fileStream.Seek(0, SeekOrigin.End);
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
        private void CreateOutputFileWithMetaData(long fileSize)
        {
            int numberOfBlocks = (int) (fileSize / _blockSize) + 1;           
            long headerSize = 2 * sizeof(int) +sizeof(long) + numberOfBlocks * sizeof(int);

            using var fileStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(headerSize);

            using var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Seek(0, SeekOrigin.Begin);

            binaryWriter.Write(numberOfBlocks);
            binaryWriter.Write(_blockSize);
            binaryWriter.Write(fileSize);
        }
        public (string message, bool success) Compress()
        {

            var fileSize = new FileInfo(_inputFileName).Length;
            CreateOutputFileWithMetaData(fileSize);

            if (fileSize < _blockSize)
            {
                return ("Split is not neccessary, file is too short", true);
            }

            var numberOfParts = fileSize / _blockSize + 1;

            var numberOfThreadsNeeded =
                numberOfParts > Environment.ProcessorCount ?
                Environment.ProcessorCount : numberOfParts;

            for (var i = 0; i < numberOfThreadsNeeded; i++)
            {
                var workingThread = new Thread(CreatePart);
                workingThread.Name = i.ToString();
                workingThread.Start();
            }
            return (numberOfThreadsNeeded.ToString(), true);
        }
    }
}
