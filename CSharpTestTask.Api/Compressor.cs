using System;
using System.Threading;
using System.IO;

namespace CSharpTestTask.Api
{
    public class Compressor
    {
        private object key = new object();
        private int _currentPartNumber;
        private readonly string _fileName;
        private readonly int _splitSize;

        private int GetNextPartNumber()
        {
            lock (key)
            {
                return _currentPartNumber++;
            }
        }
        public Compressor(string fileName)
        {
            _fileName = fileName;
            _splitSize = 1048576;
        }
        public Compressor(string fileName, int splitSize)
        {
            _fileName = fileName;
            _splitSize = splitSize;
        }
        private void CreatePart()
        {
            int readedNumberOfBytes;
            using (var fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var buffer = new byte[_splitSize];
                var partNumber = GetNextPartNumber();
                var offset = partNumber * _splitSize;
                fileStream.Seek(offset, SeekOrigin.Begin);
                Console.WriteLine($"{Thread.CurrentThread.Name}, {partNumber}, {offset}");
                readedNumberOfBytes = fileStream.Read(buffer, 0, _splitSize);
                Console.WriteLine($"{Thread.CurrentThread.Name} readed {readedNumberOfBytes} bytes");
                if (readedNumberOfBytes > 0)
                {                   
                    File.WriteAllBytes($"{_fileName}_part{partNumber}.tmp", buffer[0..(readedNumberOfBytes-1)]);
                }                
            }
            if (readedNumberOfBytes == _splitSize)
            {
                CreatePart();
            }
        }
        public (string message, bool success) Split()
        {

            var fileSize = new FileInfo(_fileName).Length;

            if (fileSize < _splitSize)
            {
                return ("Split is not neccessary, file is too short", true);
            }

            var numberOfParts = fileSize / _splitSize + 1;

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
