using System;
using System.IO;

namespace CSharpTestTask.DummyFileCreator
{
    public class FileCreator
    {
        public void CreateUnorderedFile(string fileName, int numberOfBytes)
        {
            var random = new Random();
            var data = new byte[numberOfBytes];
            random.NextBytes(data);
            File.WriteAllBytes(fileName, data);
        }
        public void CreateFileUsingChars(string fileName, int numberOfBytes, char[] chars)
        {            
            var data = new byte[numberOfBytes];
            var random = new Random();
            for (var i=0;i<numberOfBytes;i++)
            {
                data[i] = (byte)chars[random.Next(0,chars.Length)];
            }
            File.WriteAllBytes(fileName, data);
        }
    }
}
