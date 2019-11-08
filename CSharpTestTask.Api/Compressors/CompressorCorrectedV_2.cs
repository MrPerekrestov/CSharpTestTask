using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace CSharpTestTask.Api.Compressors
{
    public class CompressorCorrectedV_2 : ICompressor
    {
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _blockSize;
        private int _readedBlockNumber;
        private string _returnMessage;
        private bool _compressionIsFinished;
        private bool _returnSuccess;
        private int _numberOfBlocks;
        private FileStream _inputFileStream;
        private FileStream _outputFileStream;
        private List<Thread> _compressionThreads;
        private Semaphore _readerSemaphore;
        private Semaphore _compressorSemaphore;
        private Semaphore _writerSemaphore;
        private ConcurrentQueue<Block> _inputQueue;
        private ConcurrentDictionary<int, Block> _compressedDictionary;
        private AutoResetEvent _blockWasReaded;
        private AutoResetEvent _currentBlockWasCompressed;
        private AutoResetEvent _currentBlockWasWritten;
        private Block _compressedBlock;
        private object _blockCompressedKey = new object();
        private volatile int _currentBlockNumber;
        private static Mutex mut = new Mutex();

        public CompressorCorrectedV_2(string inputFileName, string outputFileName, int blockSize = 1048576)
        {
            _inputFilePath = inputFileName;
            _outputFilePath = outputFileName;
            _blockSize = blockSize;
        }
        public (string message, bool success) Compress()
        {
            _readedBlockNumber = 0;
            _currentBlockNumber = 0;
            _returnMessage = string.Empty;
            _compressionIsFinished = false;
            _returnSuccess = false;
            _compressionThreads = new List<Thread>();
            _compressedDictionary = new ConcurrentDictionary<int, Block>();
            _inputQueue = new ConcurrentQueue<Block>();
            _blockWasReaded = new AutoResetEvent(false);
            _currentBlockWasCompressed = new AutoResetEvent(false);
            _currentBlockWasWritten = new AutoResetEvent(false);
            var fileSize = new FileInfo(_inputFilePath).Length;
            _numberOfBlocks = (int)(fileSize / _blockSize) + 1;
            _inputFileStream = new FileStream(_inputFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            _outputFileStream = new FileStream(_outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            var numberOfThreadsNeeded =
             _numberOfBlocks > Environment.ProcessorCount ?
             Environment.ProcessorCount : _numberOfBlocks;

            _readerSemaphore = new Semaphore(numberOfThreadsNeeded, numberOfThreadsNeeded);
            _compressorSemaphore = new Semaphore(0, numberOfThreadsNeeded);
            _writerSemaphore = new Semaphore(0, numberOfThreadsNeeded);

            var readingThread = new Thread(ReadBlock);
            var writingThread = new Thread(WriteBlock);

            for (var i = 0; i < numberOfThreadsNeeded; i++)
            {
                var compressionThread = new Thread(CompressBlock);
                _compressionThreads.Add(compressionThread);
            }

            readingThread.Start();
            foreach (var compressionThread in _compressionThreads)
            {
                compressionThread.Start();
            }
            writingThread.Start();

            return ("", false);
        }
        private byte[] GZipCompress(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                using (var inputStream = new MemoryStream(data))
                    inputStream.CopyTo(gZipStream);
                return outputStream.ToArray();
            }
        }

        private void CompressBlock()
        {
            _blockWasReaded.WaitOne();
            if (_inputQueue.TryDequeue(out Block block))
            {
                var compressedBytes = GZipCompress(block.Bytes);
                var addResult = _compressedDictionary.TryAdd(block.Number, new Block(block.Number, compressedBytes));
                if (addResult)
                {
                    Console.WriteLine($"Block number {block.Number} was added");
                    lock (_blockCompressedKey)
                    {

                        while (block.Number != _currentBlockNumber)
                        {
                            Console.WriteLine($"{block.Number} {_currentBlockNumber}");
                            Monitor.Wait(_blockCompressedKey);
                        }

                        _compressedBlock = block;
                        _compressorSemaphore.Release(1);
                        _writerSemaphore.WaitOne();                                                                 
                        
                        Monitor.PulseAll(_blockCompressedKey);
                        
                    }
                }
                _readerSemaphore.Release(1);
            }
            CompressBlock();
        }

        private void WriteBlock()
        {
            _compressorSemaphore.WaitOne();
            Console.WriteLine($"block{_compressedBlock.Number} was written");
            _currentBlockNumber++;
            _writerSemaphore.Release(1);
            // WriteBlock(_compressedBlock);
            WriteBlock();

            void WriteBlockToFile(Block block)
            {
                byte[] blockNumberBytes = new byte[4];

                blockNumberBytes[0] = (byte)(block.Number >> 24);
                blockNumberBytes[1] = (byte)(block.Number >> 16);
                blockNumberBytes[2] = (byte)(block.Number >> 8);
                blockNumberBytes[3] = (byte)block.Number;

                _outputFileStream.Write(blockNumberBytes, 0, blockNumberBytes.Length);
                _outputFileStream.Write(block.Bytes, 0, block.Bytes.Length);

                Console.WriteLine($"Block {block.Number} was written");
                _currentBlockNumber++;                
                _writerSemaphore.Release(4);               
            }
        }

        private void ReadBlock()
        {
            _readerSemaphore.WaitOne();
            Console.WriteLine($"Reading block number {_readedBlockNumber}");
            var bytes = new byte[_blockSize];
            _inputFileStream.Read(bytes, 0, _blockSize);
            _inputQueue.Enqueue(new Block(_readedBlockNumber++, bytes));
            _blockWasReaded.Set();

            if (_readedBlockNumber < _numberOfBlocks)
            {
                ReadBlock();
            }
        }
    }
}
