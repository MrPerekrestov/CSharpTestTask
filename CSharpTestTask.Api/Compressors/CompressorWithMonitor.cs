﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace CSharpTestTask.Api.Compressors
{
    public class CompressorWithMonitor : ICompressor
    {
        private volatile int _currentBlockNumber = 0;
        private volatile bool _compressionIsFinished;
        private volatile string _returnMessage;
        private bool _returnSuccess;
        private volatile int _blockNumberToWrite = 0;
        private readonly object _blockNumberLock = new object();
        private readonly object _writeCompressedBlockLock = new object();
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _blockSize;
        private long _fileSize;
        private int _numberOfBlocks;

        private int GetNextBlockNumber()
        {
            lock (_blockNumberLock)
            {
                return _currentBlockNumber++;
            }
        }

        public CompressorWithMonitor(string inputFileName, string outputFileName, int blockSize = 1048576)
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
            var (buffer, blockNumber, readedNumberOfBytes) = ReadPartBytes();             

            if (readedNumberOfBytes > 0)
            {
                WriteCompressedPartToOutputFile(buffer, blockNumber, readedNumberOfBytes);
            }
            if (blockNumber < _numberOfBlocks)
            {
                ProcessBlock();
            }
        }
       
        private void WriteCompressedPartToOutputFile(byte[] bytes, int blockNumber, int readedNumberOfBytes)
        {
            var compressedBytes = CompressBlock(bytes[0..readedNumberOfBytes]);
            File.WriteAllBytes($"part_{blockNumber}", compressedBytes);
            lock (_writeCompressedBlockLock)
            {
                while (!(blockNumber == _blockNumberToWrite || _compressionIsFinished))
                    Monitor.Wait(_writeCompressedBlockLock);
              
                using (var fileStream = new FileStream(_outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                using (var writer = new BinaryWriter(fileStream))
                {
                    var headerPosition = 2 * sizeof(int) + sizeof(long) + blockNumber * sizeof(int);

                    writer.Seek(headerPosition, SeekOrigin.Begin);
                    writer.Write(compressedBytes.Length);

                    writer.Seek(0, SeekOrigin.End);
                    writer.Write(compressedBytes, 0, compressedBytes.Length);
                }               

                if (readedNumberOfBytes < _blockSize)
                {
                    _returnMessage = "Successfully compressed";
                    _returnSuccess = true;
                    _compressionIsFinished = true;
                }
                _blockNumberToWrite++;
                Monitor.PulseAll(_writeCompressedBlockLock);
            }

        }

        private (byte[] buffer, int blocktNumber, int readedNumberOfBytes) ReadPartBytes()
        {
            var buffer = new byte[_blockSize];
            var blockNumber = GetNextBlockNumber();
            var offset = blockNumber * _blockSize;
           
            int readedNumberOfBytes;
            using (var fileStream = new FileStream(_inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                readedNumberOfBytes = fileStream.Read(buffer, 0, _blockSize);
            }           

            return (buffer, blockNumber, readedNumberOfBytes);
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
            return (_returnMessage, _returnSuccess);
        }
    }
}
