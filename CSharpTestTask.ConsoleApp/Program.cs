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
            //var compressor = new Compressor("dummy_ordered.txt","dummy_ordered.smprsd");
            //var equal = FileEquals("dummy_ordered.txt.cmprsd", "as_dummy_ordered.txt");
            //Console.WriteLine(equal);
            //var result = compressor.Split();
            //compressor.AssembleBack();
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
            var decompressor = new Decompressor("dummy_ordered.smprsd", "restored_ummy_ordered.txt");
            var result = decompressor.Decompress();
        }
        static bool FileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        Console.WriteLine($"wrong byte {i}");
                        return false;
                    }
                }
                return true;
            }
            Console.WriteLine($"wrong length {file1.Length} {file2.Length}");
            return false;
        }
    }
}
