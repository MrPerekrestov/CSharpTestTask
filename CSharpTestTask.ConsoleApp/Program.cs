using CSharpTestTask.Api.Compressors;
using CSharpTestTask.Api.Decompressors;
using CSharpTestTask.Api.IOFilesCheckers;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace CSharpTestTask.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Error: 3 arguments are requiered:" +
                    " [method(compress|decompress)] [original file name] [archive file name]\r\n1");
                return;
            }

            var method = args[0];
            var inputFilePath = args[1];
            var outputFilePath = args[2];

            var fileChecker = new IOFilesChecker(inputFilePath, outputFilePath);
            var (message,success) = fileChecker.CheckFiles();
            if (!success)
            {
                Console.WriteLine($"\r\n{message}\r\n1");
                return;
            }

            switch (method)
            {
               case "compress":
                    var compressor = new CompressorWithSpinWait(inputFilePath, outputFilePath);
                    PerformCompression(compressor);
                    break;
               case "decompress":
                    var decompressor = new Decompressor(inputFilePath, outputFilePath);
                    PerformDecompression(decompressor);
                    break;
               default:                    
                    Console.WriteLine($"\r\nError: There is no such method as '{args[0]}'\r\n1");                    
                    break;
            }            
        }
        private static void PerformCompression(ICompressor compressor)
        {          
            var (message,success) = compressor.Compress();
            if (success)
            {
                Console.WriteLine("\r\n0");
                return;
            }
            Console.WriteLine($"\r\nError: {message}\r\n1");
        }
       
        private static void PerformDecompression(IDecompressor compressor)
        {
            var (message, success) = compressor.Decompress();
            if (success)
            {
                Console.WriteLine("0");
                return;
            }           
            Console.WriteLine($"\r\nError: {message}\r\n1");
        }
    }
}
