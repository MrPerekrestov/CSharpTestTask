namespace CSharpTestTask.DummyFileCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileCreator = new FileCreator();        
            fileCreator.CreateFileUsingChars("dummy.txt", 30000000,new[]{'a','b','c'});
        }
    }
}
