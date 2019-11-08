using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace CSharpTestTask.Api.Compressors
{
    public class CompressorCorrected : ICompressor
    {
        private volatile int _currentBlockNumber = 0;
        private volatile bool _compressionIsFinished;
        private volatile string _returnMessage;
        private bool _returnSuccess;
        private volatile int _blockNumberToWrite = 0;
        private readonly object _blockNumberLock = new object();
        private readonly object _writeCompressedBlockLock = new object();
        private readonly object _blockReadLock = new object();
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _blockSize;
        private long _fileSize;
        private int _numberOfBlocks;
        private FileStream _inputFileStream;

        private int GetNextBlockNumber()
        {
            lock (_blockNumberLock)
            {
                return _currentBlockNumber++;
            }
        }

        public CompressorCorrected(string inputFileName, string outputFileName, int blockSize = 1048576)
        {
            _inputFilePath = inputFileName;
            _outputFilePath = outputFileName;
            _blockSize = blockSize;
        }
        private byte[] CompressBlock(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                using (var inputStream = new MemoryStream(data))
                    inputStream.CopyTo(gZipStream);
                return outputStream.ToArray();
            }
        }
        private void ProcessBlock()
        {
            var (buffer, blockNumber, readedNumberOfBytes) = ReadBlokBytes();

            if (readedNumberOfBytes > 0)
            {
                WriteCompresseBlockToOutputFile(buffer, blockNumber, readedNumberOfBytes);
            }
            if (blockNumber < _numberOfBlocks)
            {
                ProcessBlock();
            }
        }

        private void WriteCompresseBlockToOutputFile(byte[] bytes, int blockNumber, int readedNumberOfBytes)
        {
            var compressedBytes = CompressBlock(bytes[0..readedNumberOfBytes]);
            using (var fileStream = new FileStream(_outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            using (var writer = new BinaryWriter(fileStream))
            {
                lock (_writeCompressedBlockLock)
                {
                    while (!(blockNumber == _blockNumberToWrite || _compressionIsFinished))
                        Monitor.Wait(_writeCompressedBlockLock);

                    var headerPosition = 2 * sizeof(int) + sizeof(long) + blockNumber * sizeof(int);
                    writer.Seek(headerPosition, SeekOrigin.Begin);
                    writer.Write(compressedBytes.Length);

                    fileStream.SetLength(fileStream.Length + compressedBytes.Length);
                    fileStream.Seek(-compressedBytes.Length, SeekOrigin.End);
                    _blockNumberToWrite++;
                    Monitor.PulseAll(_writeCompressedBlockLock);
                }
                fileStream.Write(compressedBytes, 0, compressedBytes.Length);
            }

            if (readedNumberOfBytes < _blockSize)
            {
                _returnMessage = "Successfully compressed";
                _returnSuccess = true;
                _compressionIsFinished = true;
            }

        }


        private (byte[] buffer, int blocktNumber, int readedNumberOfBytes) ReadBlokBytes()
        {
            var buffer = new byte[_blockSize];
            lock (_blockReadLock)
            {
                var blockNumber = GetNextBlockNumber();
                var readedNumberOfBytes = _inputFileStream.Read(buffer, 0, _blockSize);
                return (buffer, blockNumber, readedNumberOfBytes);
            }
        }

        private void CreateOutputFileWithMetaData()
        {
            long headerSize = 2 * sizeof(int) + sizeof(long) + _numberOfBlocks * sizeof(int);

            using (var fileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var binaryWriter = new BinaryWriter(fileStream))
            {
                fileStream.SetLength(headerSize);

                binaryWriter.Seek(0, SeekOrigin.Begin);

                binaryWriter.Write(_numberOfBlocks);
                binaryWriter.Write(_blockSize);
                binaryWriter.Write(_fileSize);
            }
        }

        public (string message, bool success) Compress()
        {
            _currentBlockNumber = 0;
            _blockNumberToWrite = 0;
            _returnMessage = string.Empty;
            _compressionIsFinished = false;
            _returnSuccess = false;
            _fileSize = new FileInfo(_inputFilePath).Length;
            _numberOfBlocks = (int)(_fileSize / _blockSize) + 1;
            _inputFileStream = new FileStream(_inputFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

            CreateOutputFileWithMetaData();

            var numberOfThreadsNeeded =
                _numberOfBlocks > Environment.ProcessorCount ?
                Environment.ProcessorCount : _numberOfBlocks;

            for (var i = 0; i < numberOfThreadsNeeded; i++)
            {
                var workingThread = new Thread(ProcessBlock);
                workingThread.Start();
            }

            SpinWait.SpinUntil(() => _compressionIsFinished);
            _inputFileStream.Close();
            return (_returnMessage, _returnSuccess);
        }
    }
}
