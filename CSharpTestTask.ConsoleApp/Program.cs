using CSharpTestTask.Api.Compressors;
using CSharpTestTask.Api.Decompressors;
using System;
using System.IO;
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
               
            }

            var method = args[0];
            var inputFileName = args[1];
            var outputFileName = args[2];

            switch (method)
            {
               case "compress":break;
               case "decompress":break;
               default: 
                    Console.WriteLine($"Error: There is no such method as '{args[0]}'\r\n1");                    
                    break;
            }            
        }
        private static void PerformCompression(ICompressor compressor)
        {
            var (message,success) = compressor.Compress();
            if (success)
            {
                Console.WriteLine("0");
                return;
            }
            Console.WriteLine($"Error: {message}\r\n1");
        }
    }
}
