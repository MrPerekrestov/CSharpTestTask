using CSharpTestTask.Api;
using System;
using System.IO;
using System.Threading;

namespace CSharpTestTask.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var compressor = new Compressor("dummy.txt");
            var result = compressor.Split();
            //using (var fileStream = new FileStream("dummy.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
            //{
            //    var buffer = new byte[1048576];
            //    var partNumber = 1;
            //    var offset = partNumber * 1048576;
            //    fileStream.Seek(offset, SeekOrigin.Begin);
            //    Console.WriteLine($"{Thread.CurrentThread.Name}, {partNumber}, {offset}");
            //    var readedNumberOfBytes = fileStream.Read(buffer, 0, 1048576);
            //    Console.WriteLine($"{Thread.CurrentThread.Name} readed {readedNumberOfBytes} bytes");
            //    File.WriteAllBytes($"{"dummy.txt"}_part{partNumber}.tmp", buffer);
            //}

        }
    }
}
