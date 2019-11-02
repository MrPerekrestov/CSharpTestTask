using System;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace CSharpTestTask.Tests
{
    class Program
    {
        static void Main(string[] args)
        {          
            var bytes = Compress(File.ReadAllBytes("dummy_ordered.txt"));
            var decompressed = Decompress(bytes);
            Console.WriteLine($"{bytes.Length} {decompressed.Length}");
            File.WriteAllBytes("decompressed_dummy_ordered.txt", decompressed);
            Console.WriteLine(FileEquals("dummy_ordered.txt", "decompressed_dummy_ordered.txt"));
            //FileMatchRegExTest();
        }

        private static void FileMatchRegExTest()
        {
            var _fileName = "asdasd";
            var regEx = new Regex($"^{_fileName}_part\\d+\\.tmp$");
            var teststring = "asdasd_part12.tmpd";
            Console.WriteLine(regEx.IsMatch(teststring));
        }
        private static byte[] ReadFromFile(string fileName) => File.ReadAllBytes(fileName);      

        static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
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
