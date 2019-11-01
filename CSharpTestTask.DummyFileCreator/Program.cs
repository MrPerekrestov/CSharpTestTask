using System;

namespace CSharpTestTask.DummyFileCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileCreator = new FileCreator();
            fileCreator.CreateFile("dummy.txt", 5000000);
        }
    }
}
