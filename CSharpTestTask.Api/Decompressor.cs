using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CSharpTestTask.Api
{
    public class Decompressor
    {
        private readonly string _inputFileName;
        private readonly string _outputFileName;

        private int _numberOfBlocks;
        private int _blockSize;
        private long _outputFileSize;
        private ConcurrentQueue<DecompressorBlockInfo> _compressedBlockInfos;

        public Decompressor(string inputFileName, string outputFileName)
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
            for (var i = 0; i < _numberOfBlocks; i++)
            {
                var numberOfBytes = binaryReader.ReadInt32();
                _compressedBlockInfos.Enqueue(
                    new DecompressorBlockInfo() {
                        Number = i,
                        NumerOfBytes = numberOfBytes,
                        Position = position
                    }) ;
                position += numberOfBytes;
            }         
        }
        private void CreateOutputFile()
        {
            using var fileStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(_outputFileSize);
        }
        private void AttachBlock()
        {
            if (_compressedBlockInfos.TryDequeue(out var blockInfo))
            {
               
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

            return ("asdasd", true);
        }

        
    }
}
