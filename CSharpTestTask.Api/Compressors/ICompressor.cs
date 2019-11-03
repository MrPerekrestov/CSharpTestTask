namespace CSharpTestTask.Api.Compressors
{
    public interface ICompressor
    {
        (string message, bool success) Compress();
    }
}