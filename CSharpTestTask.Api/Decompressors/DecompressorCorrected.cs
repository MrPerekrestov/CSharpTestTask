using CSharpTestTask.Api.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace CSharpTestTask.Api.Decompressors
{
    public class DecompressorCorrected : IDecompressor,IDisposable
    {
        private readonly string _inputFilePath;
        private readonly string _outputFilePath;
        private volatile int _blockNumber;
        private FileStream _inputFileStream;
        private FileStream _outputFileStream;
        private Semaphore _readerSemaphore;
        private AutoResetEvent _readyToDecompress;
        private AutoResetEvent _currentBlockDecompressed;
        private AutoResetEvent _currentBlockWritten;
        private Block _decompressedBlock;
        private ConcurrentQueue<Block> _compressedBlocks;
        private int _numberOfCompressionThreads;
        private List<Thread> _decompressionThreads;
        private object _blockDecompressedKey = new object();
        private volatile int _currenBlockNumber;
        private volatile bool _readingIsFinished;
        private volatile bool _writingIsFinished;

        public DecompressorCorrected(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
        }
        public (string message, bool success) Decompress()
        {
            SetInitialValues();

            var readingThread = new Thread(ReadBlock);
            var writingThread = new Thread(WriteBlock);
            _decompressionThreads = new List<Thread>();
            for (var i = 0; i < _numberOfCompressionThreads; i++)
            {
                var compressionThread = new Thread(DecompressBlock);
                _decompressionThreads.Add(compressionThread);
                compressionThread.Start();
            }
            readingThread.Start();
            writingThread.Start();

            readingThread.Join();
            writingThread.Join();

            foreach (var decompressionThread in _decompressionThreads)
            {
                decompressionThread.Join();
            }           
            return ("Successfully decompressed", true);
        }

        private void SetInitialValues()
        {
            _blockNumber = 0;
            _currenBlockNumber = 0;
            _readingIsFinished = false;
            _writingIsFinished = false;
            _inputFileStream = new FileStream(_inputFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            _outputFileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            _readerSemaphore = new Semaphore(Environment.ProcessorCount, Environment.ProcessorCount);
            _readyToDecompress = new AutoResetEvent(false);
            _currentBlockDecompressed = new AutoResetEvent(false);
            _currentBlockWritten = new AutoResetEvent(false);
            _compressedBlocks = new ConcurrentQueue<Block>();
            _numberOfCompressionThreads = Environment.ProcessorCount;
        }

        private void DecompressBlock()
        {         
            while(!_writingIsFinished)
            {
                _readyToDecompress.WaitOne();
                if (_compressedBlocks.TryDequeue(out Block block))
                {
                    var decompressedBlockBytes = Decompress(block.Bytes);
                    lock (_blockDecompressedKey)
                    {
                        while (_currenBlockNumber != block.Number)
                        {
                            Monitor.Wait(_blockDecompressedKey);
                        }
                        _decompressedBlock = new Block(_currenBlockNumber, decompressedBlockBytes);
                        _currentBlockDecompressed.Set();
                        _currentBlockWritten.WaitOne();

                        Monitor.PulseAll(_blockDecompressedKey);

                        if (_writingIsFinished)
                        {
                            for (var i = 0; i < _numberOfCompressionThreads-1; i++)
                            {
                                _readyToDecompress.Set();                               
                            }                           
                            return;
                        }
                        _currenBlockNumber++;
                        
                    }
                    _readerSemaphore.Release();                    
                }
            }           
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

        private void WriteBlock()
        {
            while(!_writingIsFinished)
            {
                _currentBlockDecompressed.WaitOne();
                _outputFileStream.Write(_decompressedBlock.Bytes, 0, _decompressedBlock.Bytes.Length);
                if (_readingIsFinished && _blockNumber == (_decompressedBlock.Number + 1))
                {                    
                    _writingIsFinished = true;
                    _currentBlockWritten.Set();
                    return;
                }
                _currentBlockWritten.Set();
            }           
        }

        private void ReadBlock()
        {
            while (!_readingIsFinished)
            {
                _readerSemaphore.WaitOne();
                var blockSize = GetBlockSize();
                if (blockSize == 0)
                {
                    _readingIsFinished = true;
                    for (var i = 0; i < _numberOfCompressionThreads; i++)
                    {
                        _readyToDecompress.Set();
                    }
                    return;
                }
                var blockBytes = GetBlockBytes(blockSize);               
                _compressedBlocks.Enqueue(new Block(_blockNumber++, blockBytes));               
                _readyToDecompress.Set();
            }           

            int GetBlockSize()
            {
                var intBytes = new byte[4];
                _inputFileStream.Read(intBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(intBytes);
                }
                return BitConverter.ToInt32(intBytes);
            }

            byte[] GetBlockBytes(int blockSize)
            {
                var blockBytes = new byte[blockSize];
                _inputFileStream.Read(blockBytes, 0, blockSize);
                return blockBytes;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _inputFileStream?.Dispose();
                    _outputFileStream?.Dispose();
                    _readyToDecompress?.Dispose();
                    _readerSemaphore?.Dispose();
                    _currentBlockWritten?.Dispose();
                    _currentBlockDecompressed?.Dispose();
                }  
                disposedValue = true;
            }
        }  
        void IDisposable.Dispose()
        {            
            Dispose(true);            
        }
        #endregion
    }
}
