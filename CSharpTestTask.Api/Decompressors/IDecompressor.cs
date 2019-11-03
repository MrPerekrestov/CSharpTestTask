namespace CSharpTestTask.Api.Decompressors
{
    public interface IDecompressor
    {
        (string message, bool success) Decompress();
    }
}