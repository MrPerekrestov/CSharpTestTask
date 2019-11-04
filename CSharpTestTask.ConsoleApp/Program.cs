using CSharpTestTask.Api.Compressors;
using CSharpTestTask.Api.Decompressors;
using CSharpTestTask.Api.IOFilesCheckers;
using System;

namespace CSharpTestTask.ConsoleApp
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Error: 3 arguments are requiered:" +
                    " [method(compress|decompress)] [original file name] [archive file name]");
                return 1;
            }

            var method = args[0];
            var inputFilePath = args[1];
            var outputFilePath = args[2];

            var fileChecker = new IOFilesChecker(inputFilePath, outputFilePath);
            var (message,success) = fileChecker.CheckFiles();
            if (!success)
            {
                Console.WriteLine($"\r\n{message}");
                return 1;
            }

            switch (method)
            {
               case "compress":
                    var compressor = new CompressorWithSpinWait(inputFilePath, outputFilePath);
                    return PerformCompression(compressor);                    
               case "decompress":
                    var decompressor = new Decompressor(inputFilePath, outputFilePath);
                    return PerformDecompression(decompressor);                    
               default:                    
                    Console.WriteLine($"\r\nError: There is no such method as '{args[0]}'");
                    return 1;                    
            }            
        }
        private static int PerformCompression(ICompressor compressor)
        {          
            var (message,success) = compressor.Compress();
            if (success)
            {
                return 0;               
            }
            Console.WriteLine($"\r\nError: {message}");
            return 1;
        }
       
        private static int PerformDecompression(IDecompressor compressor)
        {
            var (message, success) = compressor.Decompress();
            if (success)
            {
                return 0;                
            }           
            Console.WriteLine($"\r\nError: {message}");
            return 1;
        }
    }
}
