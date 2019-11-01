using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSharpTestTask.DummyFileCreator
{
    public class FileCreator
    {
        public void CreateFile(string fileName, int numberOfBytes)
        {
            var random = new Random();
            var data = new byte[numberOfBytes];
            random.NextBytes(data);
            File.WriteAllBytes(fileName, data);
        }
    }
}
