using System.Runtime.CompilerServices;

namespace DDSTextureLoader.NET
{
    internal struct Size2
    {
        public Size2(uint height, uint width)
        {
            Height = height;
            Width = width;
        }

        public void Deconstruct(out uint height, out uint width)
        {
            height = Height;
            width = Width;
        }

        public uint Height { get; set; }
        public uint Width { get; set; }
    }

    internal struct Size3
    {
        public Size3(uint height, uint width, uint depth)
        {
            Height = height;
            Width = width;
            Depth = depth;
        }

        public void Deconstruct(out uint height, out uint width, out uint depth)
        {
            height = Height;
            width = Width;
            depth = Depth;
        }

        public static explicit operator Size2(Size3 size) => Unsafe.As<Size3, Size2>(ref size);

        public uint Height { get; set; }
        public uint Width { get; set; }
        public uint Depth { get; set; }
    }
}