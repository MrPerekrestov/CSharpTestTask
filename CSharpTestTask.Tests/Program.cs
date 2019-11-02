using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Runtime.ConstrainedExecution;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace CSharpTestTask.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            //var bytes = Compress(File.ReadAllBytes("dummy_ordered.txt"));
            //var decompressed = Decompress(bytes);
            //Console.WriteLine($"{bytes.Length} {decompressed.Length}");
            //File.WriteAllBytes("decompressed_dummy_ordered.txt", decompressed);
            //Console.WriteLine(FileEquals("dummy_ordered.txt", "decompressed_dummy_ordered.txt"));
            //FileMatchRegExTest();  
            //CreateBlankFileWithHeader("textfilewithheader.txt");
            //  CreateEmptyFileWithGivenSize("test_file.txt", 10000);
            var result = ReadMetaData("dummy_ordered.smprsd");
            Console.WriteLine($"{result.fileSize} {result.numberOfParts} {result.partSize}");
            foreach(var partSize in result.compressedPartSizes)
            {
                Console.WriteLine(partSize);
            }
        }
        private static void CreateBlankFileWithHeader(string fileName)
        {
            long numberOfParts = 4;
            long partSize = 100000;
            long lastPartSize = 50000;
            long headerSize = 3 * sizeof(long) + numberOfParts * sizeof(long);            
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(headerSize);
            using var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Seek(0, SeekOrigin.Begin);
            binaryWriter.Write(numberOfParts);
            binaryWriter.Write(partSize);
            binaryWriter.Write(lastPartSize);
        }
        private static (int numberOfParts, int partSize, long fileSize, List<int> compressedPartSizes) ReadMetaData(string fileName)
        {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            using var binaryReader = new BinaryReader(fileStream);
            var numberOfParts = binaryReader.ReadInt32();
            var partSize = binaryReader.ReadInt32();
            var fileSize = binaryReader.ReadInt64();
            var compressedPartSizes = new List<int>();
            for (var i=0; i<numberOfParts; i++)
            {
                compressedPartSizes.Add(binaryReader.ReadInt32());
            }
            return (numberOfParts, partSize, fileSize, compressedPartSizes);
        }

        private static void FileMatchRegExTest()
        {
            var _fileName = "asdasd";
            var regEx = new Regex($"^{_fileName}_part\\d+\\.tmp$");
            var teststring = "asdasd_part12.tmpd";
            Console.WriteLine(regEx.IsMatch(teststring));
        }
        private static void CreateEmptyFileWithGivenSize(string fileName, long size)
        {
            using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            fileStream.SetLength(size);
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
