namespace CSharpTestTask.Api.IOFilesCheckers
{
    public interface IIOFilesChecker
    {
        (string message, bool success) CheckFiles();
    }
}