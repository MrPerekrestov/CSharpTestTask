namespace CSharpTestTask.DummyFileCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileCreator = new FileCreator();        
            fileCreator.CreateFileUsingChars("dummy_ordered.txt", 900000,new[]{'a','b','c'});
        }
    }
}
