using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using CSharpTestTask.Api.Shared;

namespace CSharpTestTask.Api.Compressors
{
    public class CompressorCorrected : ICompressor, IDisposable
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
        private AutoResetEvent _readyToCompress;
        private AutoResetEvent _readyToWrite;
        private AutoResetEvent _currentBlockIsWritten;
        private Block _compressedBlock;
        private object _blockCompressedKey = new object();
        private volatile int _nextBlockNumber;
        private volatile bool _compressionIsFinished;

        public CompressorCorrected(string inputFileName, string outputFileName, int blockSize = 1048576)
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
            _readyToCompress = new AutoResetEvent(false);
            _readyToWrite = new AutoResetEvent(false);
            _currentBlockIsWritten = new AutoResetEvent(false);
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
            while (!_compressionIsFinished)
            {
                _readyToCompress.WaitOne();

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

                        _readyToWrite.Set();
                        _currentBlockIsWritten.WaitOne();
                        Monitor.PulseAll(_blockCompressedKey);

                        if (_compressionIsFinished)
                        {
                            for (var i = 0; i < _numberOfCompresionThreads; i++)
                            {
                                _readyToCompress.Set();
                            }
                            return;
                        }
                    }
                    _readerSemaphore.Release();
                }
            }
        }

        private void WriteBlock()
        {
            while (!_compressionIsFinished)
            {
                _readyToWrite.WaitOne();

                byte[] blockNumberLengthBytes = BitConverter.GetBytes(_compressedBlock.Bytes.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(blockNumberLengthBytes);
                }
               
                _outputFileStream.Write(blockNumberLengthBytes, 0, blockNumberLengthBytes.Length);
                _outputFileStream.Write(_compressedBlock.Bytes, 0, _compressedBlock.Bytes.Length);
                _nextBlockNumber++;               

                _currentBlockIsWritten.Set();

                if (_nextBlockNumber == _numberOfBlocks)
                {
                    _compressionIsFinished = true;
                    return;
                }
            }
        }

        private void ReadBlock()
        {
            while (_readedBlockNumber < _numberOfBlocks)
            {
                _readerSemaphore.WaitOne();
                var bytes = new byte[_blockSize];
               
                var readedBytes = _inputFileStream.Read(bytes, 0, _blockSize);
              
                if (readedBytes > 0)
                {
                    _inputQueue.Enqueue(new Block(_readedBlockNumber++, bytes[0..readedBytes]));
                    _readyToCompress.Set();
                }
            }
            for (var i = 0; i < _numberOfCompresionThreads; i++)
            {
                _readyToCompress.Set();
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
                    _readerSemaphore?.Dispose();
                    _readyToCompress?.Dispose();
                    _readyToWrite?.Dispose();
                    _currentBlockIsWritten?.Dispose();
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
