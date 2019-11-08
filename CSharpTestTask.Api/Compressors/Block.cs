namespace CSharpTestTask.Api.Compressors
{
    internal class Block
    {
        public Block(int number, byte[] data)
        {
            Number = number;
            Bytes = data;
        }
        public int Number { get; set; }
        public byte[] Bytes { get; set; }
    }
}