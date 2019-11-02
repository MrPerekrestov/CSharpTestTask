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
        private byte[] Compress(byte[] data)
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
            var (bytes, partNumber, readedNumberOfBytes) = ReadPartBytes();
            if (readedNumberOfBytes > 0)
            {
                WriteCompressedPartToOutputFile(bytes, partNumber, readedNumberOfBytes);
            }            
            if (readedNumberOfBytes == _splitSize)
            {
                CreatePart();
            }
        }
        private void WriteCompressedPartToOutputFile(byte[] bytes, int partNumber, int readedNumberOfBytes)
        {
            SpinWait.SpinUntil(() => partNumber == _partNumberToWrite);
            var compressedBytes = Compress(bytes[0..readedNumberOfBytes]);
            File.WriteAllBytes($"{_fileName}_part{partNumber}.tmp", compressedBytes);
            using var fileStream = new FileStream($"{_fileName}.cmprsd", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            fileStream.Seek(0, SeekOrigin.End);
            fileStream.Write(compressedBytes, 0, compressedBytes.Length);
            Console.WriteLine($"{partNumber} was written");
            _partNumberToWrite++;
        }

        private (byte[] Data, int PartNumber, int readedNumberOfBytes) ReadPartBytes()
        {
            using var fileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[_splitSize];
            var partNumber = GetNextPartNumber();
            var offset = partNumber * _splitSize;
            fileStream.Seek(offset, SeekOrigin.Begin);
            Console.WriteLine($"{Thread.CurrentThread.Name}, {partNumber}, {offset}");
            var readedNumberOfBytes = fileStream.Read(buffer, 0, _splitSize);
            Console.WriteLine($"{Thread.CurrentThread.Name} readed {readedNumberOfBytes} bytes");           
            return (buffer,partNumber,readedNumberOfBytes);
        }

        public void AssembleBack()
        {
            var regEx = new Regex($"^{_fileName}_part\\d+\\.tmp$");
            var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory());
            var filesToAssemble = allFiles.Where(file => regEx.IsMatch(file.Split("\\").LastOrDefault())).ToList();
            foreach (var file in filesToAssemble)
            {
                var bytes = File.ReadAllBytes(file);
                using (var fileStream = new FileStream($"as_{_fileName}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    fileStream.Seek(0, SeekOrigin.End);
                    fileStream.Write(bytes, 0, bytes.Length);
                }
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
