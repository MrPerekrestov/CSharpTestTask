using CSharpTestTask.Api.Compressors;
using CSharpTestTask.Api.Decompressors;
using CSharpTestTask.DummyFileCreator;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;


namespace CSharpTestTask.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($".NET Core version:\t{GetNetCoreVersion()}\r\n");
            var fileCreator = new FileCreator();
            var inputFileName = "inputFile";
            var outputFileName = "inputFile_compressed";
            var restoredFileName = "inputFile_restored";
            ThreeSymbols50MBTest(fileCreator, inputFileName, outputFileName, restoredFileName);
           
        }
        private static byte[] Decompress(byte[] data)
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

        private static void ThreeSymbols50MBTest(FileCreator fileCreator, string inputFileName, string outputFileName, string restoredFileName)
        {
            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Three different symbols  50mb test");
            Console.WriteLine(new string('-', 40));
           // fileCreator.CreateFileUsingChars(inputFileName, 50000000, new[] { 's', 'f', 'a' });
            Console.WriteLine("Input file size:\t50000000 bytes");
            var compressor = new CompressorCorrected(inputFileName, outputFileName);
            var decompressor = new DecompressorCorrected(outputFileName, restoredFileName);
            var result = Test(compressor, decompressor, inputFileName, outputFileName, restoredFileName);
            Console.WriteLine($"Equality test:\t\t{result}");           
            File.Delete(outputFileName);
            File.Delete(restoredFileName);
            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Files were deleted");
        }

        static bool Test(ICompressor compressor, IDecompressor decompressor, string inputFileName, string outputFileName, string restoredFileName)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var compressResult = compressor.Compress();
            stopWatch.Stop();
            if (compressor is IDisposable disposableCompressor)
            {
                disposableCompressor.Dispose();
            }
            var compressionTime = stopWatch.ElapsedMilliseconds;
            if (!compressResult.success) return false;
            Console.WriteLine($"Output file size:\t{new FileInfo(outputFileName).Length} bytes");

            stopWatch.Reset();
            stopWatch.Start();
            var decompressResult = decompressor.Decompress();
            stopWatch.Stop();
            if (decompressor is IDisposable disposableDecompressor)
            {
                disposableDecompressor.Dispose();
            }
            var decompressionTime = stopWatch.ElapsedMilliseconds;
            if (!decompressResult.success) return false;
            Console.WriteLine($"Restored file size:\t{new FileInfo(restoredFileName).Length} bytes");
            Console.WriteLine($"Compression time:\t{compressionTime} ms");
            Console.WriteLine($"Decompression time:\t{decompressionTime} ms");
            return FileEquals(inputFileName, restoredFileName);
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
                        Console.WriteLine($"{i} byte is not the same");
                        return false;
                    }
                }
                return true;
            }
            Console.WriteLine("length is not the same");
            return false;
        }
        public static string GetNetCoreVersion()
        {
            var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return null;
        }

    }
}
