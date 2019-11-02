using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpTestTask.DummyFileCreator
{
    public class FileCreator
    {
        public void CreateFileUnorderedFile(string fileName, int numberOfBytes)
        {
            var random = new Random();
            var data = new byte[numberOfBytes];
            random.NextBytes(data);
            File.WriteAllBytes(fileName, data);
        }
        public void CreateOrderdFile(string fileName, int numberOfBytes, char ch)
        {            
            var data = new byte[numberOfBytes];
            for (var i=0;i<numberOfBytes;i++)
            {
                data[i] = (byte)ch;
            }
            File.WriteAllBytes(fileName, data);
        }
    }
}
