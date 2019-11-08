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
    public class CompressorCorrectedV_2 : ICompressor, IDisposable
    {
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private readonly int _blockSize;
        private int _readedBlockNumber;     
        private int _numberOfBlocks;
        private FileStream _inputFileStream;
        private FileStream _outputFileStream;
        private List<Thread> _compressionThreads;
        private int _numberOfCompresionThreads;
        private Semaphore _readerSemaphore;      
        private ConcurrentQueue<Block> _inputQueue;        
        private AutoResetEvent _blockIsReaded;
        private AutoResetEvent _currentBlockIsCompressed;
        private AutoResetEvent _currentBlockIsWritten;
        private AutoResetEvent _compressionIsFinished;
        private Block _compressedBlock;
        private object _blockCompressedKey = new object();
        private volatile int _nextBlockNumber;     
        private volatile bool _allBlocksWereWritten;

        public CompressorCorrectedV_2(string inputFileName, string outputFileName, int blockSize = 1048576)
        {
            _inputFilePath = inputFileName;
            _outputFilePath = outputFileName;
            _blockSize = blockSize;

        }
        public (string message, bool success) Compress()
        {
            SetInitialValues();            

            var readingThread = new Thread(ReadBlock);
            var writingThread = new Thread(WriteBlock);

            _compressionThreads = new List<Thread>();
            for (var i = 0; i < _numberOfCompresionThreads; i++)
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

            _compressionIsFinished.WaitOne();

            readingThread.Join();
            writingThread.Join();
            foreach (var compressionThread in _compressionThreads)
            {
                compressionThread.Join();
            }
            return ("Successfully finished", true);
        }

        private void SetInitialValues()
        {
            _readedBlockNumber = 0;
            _nextBlockNumber = 0;            
            _inputQueue = new ConcurrentQueue<Block>();           

            var fileSize = new FileInfo(_inputFilePath).Length;
            _numberOfBlocks = (int)(fileSize / _blockSize) + 1;

            _inputFileStream = new FileStream(_inputFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            _outputFileStream = new FileStream(_outputFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

            _numberOfCompresionThreads =
             _numberOfBlocks > Environment.ProcessorCount ?
             Environment.ProcessorCount : _numberOfBlocks;

            _readerSemaphore = new Semaphore(_numberOfCompresionThreads, _numberOfCompresionThreads);
            _blockIsReaded = new AutoResetEvent(false);
            _currentBlockIsCompressed = new AutoResetEvent(false);
            _currentBlockIsWritten = new AutoResetEvent(false);
            _compressionIsFinished = new AutoResetEvent(false);
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
             _blockIsReaded.WaitOne();

            if (_inputQueue.TryDequeue(out Block block))
            {
                var compressedBytes = GZipCompress(block.Bytes);

                lock (_blockCompressedKey)
                {
                    while (block.Number != _nextBlockNumber)
                    {
                        Monitor.Wait(_blockCompressedKey);
                    }
                    _compressedBlock = new Block(block.Number, compressedBytes);

                    _currentBlockIsCompressed.Set();

                    _currentBlockIsWritten.WaitOne();

                    Monitor.PulseAll(_blockCompressedKey);
                }
            }

            if (!_allBlocksWereWritten)
            {
                _readerSemaphore.Release(1);
                 CompressBlock();
            }
            else
            {
                for (var i=0;i< _numberOfCompresionThreads; i++)
                _blockIsReaded.Set();
                _compressionIsFinished.Set();
                return;
            }
        }

        private void WriteBlock()
        {           
            _currentBlockIsCompressed.WaitOne();
            WriteBlockToFile(_compressedBlock);

            Console.WriteLine($"block{_compressedBlock.Number} was written");

            _nextBlockNumber++;

            _currentBlockIsWritten.Set();

            if (_nextBlockNumber == _numberOfBlocks)
            {               
                _allBlocksWereWritten = true;                
                return;
            }
           
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
            }
        }

        private void ReadBlock()
        {
            _readerSemaphore.WaitOne();
            Console.WriteLine($"Reading block number {_readedBlockNumber}");
            var bytes = new byte[_blockSize];
            _inputFileStream.Read(bytes, 0, _blockSize);
            _inputQueue.Enqueue(new Block(_readedBlockNumber++, bytes));
            _blockIsReaded.Set();

            if (_readedBlockNumber < _numberOfBlocks)
            {
                ReadBlock();
            }
            else
            {
                Console.WriteLine("All blocks were readed");
            }
        }
    
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _inputFileStream?.Dispose();
                    _outputFileStream?.Dispose();
                    _readerSemaphore?.Dispose();
                    _blockIsReaded?.Dispose();
                    _compressionIsFinished?.Dispose();
                    _currentBlockIsCompressed?.Dispose();
                    _currentBlockIsWritten?.Dispose();
                }              

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {          
            Dispose(true);           
        }
        
    }
}
