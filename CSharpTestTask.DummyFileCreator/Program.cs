using System;

namespace CSharpTestTask.DummyFileCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileCreator = new FileCreator();
            //fileCreator.CreateFileUnorderedFile("dummy.txt", 5000000);
            fileCreator.CreateOrderdFile("dummy_ordered.txt", 900000,new[]{'a','b','c'});
        }
    }
}
