﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace CSharpTestTask.Api.Decompressors
{
    public class TestDecompressor : IDecompressor
    {
        private readonly string _inputFileName;
        private readonly string _outputFileName;

        private int _numberOfBlocks;
        private int _blockSize;
        private long _outputFileSize;
        private ConcurrentQueue<DecompressorBlockInfo> _compressedBlockInfos;
        private bool _decompressionIsFinished = false;
        private string _returnMessage = string.Empty;
        private bool _returnSuccess = false;

        public TestDecompressor(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
        }
        private void ReadMetaData()
        {
            using var fileStream = new FileStream(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.None);
            using var binaryReader = new BinaryReader(fileStream);
            _numberOfBlocks = binaryReader.ReadInt32();
            _blockSize = binaryReader.ReadInt32();
            _outputFileSize = binaryReader.ReadInt64();
            _compressedBlockInfos = new ConcurrentQueue<DecompressorBlockInfo>();
            long position = 2 * sizeof(int) + sizeof(long) + _numberOfBlocks * sizeof(int);
            Console.WriteLine($"Number of blocks = {_numberOfBlocks}");
            Console.WriteLine($"Block size {_blockSize}");
            Console.WriteLine($"Output file size {_outputFileSize}");
            Console.WriteLine(new string('x', 40));
            for (var i = 0; i < _numberOfBlocks; i++)
            {
                if (i != 0) return;
                var numberOfBytes = binaryReader.ReadInt32();
                _compressedBlockInfos.Enqueue(
                    new DecompressorBlockInfo()
                    {
                        Number = i,
                        NumerOfBytes = numberOfBytes,
                        Position = position
                    });
                Console.WriteLine($"Block position = {position}");
                Console.WriteLine($"Block number number of bytes = {numberOfBytes}");
                position += numberOfBytes;
            }

        }
        private void CreateOutputFile()
        {
            using var fileStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(_outputFileSize);
        }
        private byte[] Decompress(byte[] data)
        {
            byte[] bytes;
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                bytes = resultStream.ToArray();
            }
            return bytes;
        }
        private void AttachBlock()
        {
            if (_compressedBlockInfos.TryDequeue(out var blockInfo))
            {
                Console.WriteLine($"trying to decompress bock {blockInfo.Number}");
                var compressedBytes = new byte[blockInfo.NumerOfBytes];
                using (var inputFileStream = new FileStream(_inputFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                {
                    inputFileStream.Seek(blockInfo.Position, SeekOrigin.Begin);
                    inputFileStream.Read(compressedBytes, 0, blockInfo.NumerOfBytes);
                }

                var decompressedBytes = Decompress(compressedBytes);
                Console.WriteLine($"block '{blockInfo.Number}' was decompressed to {decompressedBytes.Length}");
                using (var outputFileStream = new FileStream(_outputFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    var position = blockInfo.Number * _blockSize;
                    outputFileStream.Seek(position, SeekOrigin.Begin);
                    outputFileStream.Write(decompressedBytes, 0, decompressedBytes.Length);
                }
                if (decompressedBytes.Length < _blockSize)
                {
                    _returnMessage = "Decompression was successfully finished";
                    _returnSuccess = true;
                    _decompressionIsFinished = true;
                }
            }

            if (_compressedBlockInfos.Count > 0)
            {
                AttachBlock();
            }
        }
        public (string message, bool success) Decompress()
        {
            ReadMetaData();

            CreateOutputFile();

            var numberOfThreadsNeeded = _numberOfBlocks > Environment.ProcessorCount ?
               Environment.ProcessorCount : _numberOfBlocks;

            for (var i = 0; i < numberOfThreadsNeeded; i++)
            {
                var workingThread = new Thread(AttachBlock);
                workingThread.Name = i.ToString();
                workingThread.Start();
            }
            SpinWait.SpinUntil(() => _decompressionIsFinished);
            return (_returnMessage, _returnSuccess);
        }

    }
}
