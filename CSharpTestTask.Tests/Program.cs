using CSharpTestTask.DummyFileCreator;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Runtime.ConstrainedExecution;
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
                        return false;
                    }
                }
                return true;
            }            
            return false;
        }

    }
}
