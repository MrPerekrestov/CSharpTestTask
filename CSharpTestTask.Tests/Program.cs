using CSharpTestTask.Api.Compressors;
using CSharpTestTask.Api.Decompressors;
using CSharpTestTask.DummyFileCreator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace CSharpTestTask.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileCreator = new FileCreator();
            var inputFileName = "inputFile";
            var outputFileName = "inputFile_compressed";
            var restoredFileName = "inputFile_restored";
            UnorderedFileTest(fileCreator, inputFileName, outputFileName, restoredFileName);
            OrderedFileTest(fileCreator, inputFileName, outputFileName, restoredFileName);
        }

        private static void UnorderedFileTest(FileCreator fileCreator, string inputFileName, string outputFileName, string restoredFileName)
        {
            Console.WriteLine("unouteder file 10mb test");
            fileCreator.CreateUnorderedFile(inputFileName, 700000);           
            var compressor = new CompressorWithMonitor(inputFileName, outputFileName);
            var decompressor = new Decompressor(outputFileName, restoredFileName);
            var result = Test(compressor, decompressor, inputFileName, outputFileName, restoredFileName);
            Console.WriteLine(result);
            Console.WriteLine(FileEquals(inputFileName, restoredFileName));
        }
        private static void OrderedFileTest(FileCreator fileCreator, string inputFileName, string outputFileName, string restoredFileName)
        {
            Console.WriteLine("Ordered file 10mb test");           
            fileCreator.CreateOrderdFile(inputFileName, 900000, new[] { 's', 'f', 'a' });
            var compressor = new CompressorWithMonitor(inputFileName, outputFileName);
            var decompressor = new Decompressor(outputFileName, restoredFileName);
            var result = Test(compressor, decompressor, inputFileName, outputFileName, restoredFileName);
            Console.WriteLine(result);
            Console.WriteLine(FileEquals(inputFileName, restoredFileName));
        }

        static bool Test(ICompressor compressor,IDecompressor decompressor, string inputFileName, string outputFileName, string restoredFileName)
        {
            var compressResult = compressor.Compress();
            if (!compressResult.success) return false;
            var decompressResult = decompressor.Decompress();
            if (!decompressResult.success) return false;
            var filesEquals  = FileEquals(inputFileName, restoredFileName);
            if (filesEquals == false) Console.WriteLine("files are not equal");
            return true;
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

    }
}
