using System.Runtime.CompilerServices;

namespace DDSTextureLoader.NET
{
    public readonly struct Size2
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

        public uint Height { get; }
        public uint Width { get; }
    }

    public readonly struct Size3
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

        internal Size3 Halve()
        {

        }

        public static explicit operator Size2(Size3 size) => Unsafe.As<Size3, Size2>(ref size);

        public uint Height { get; }
        public uint Width { get; }
        public uint Depth { get; }
    }

    internal struct Size4
    {
        public uint Height, Width, Depth, Reserved;

        public Size4(uint height, uint width, uint depth, uint reserved)
        {
            Height = height;
            Width = width;
            Depth = depth;
            Reserved = reserved;
        }

        public static explicit operator Size4(Size3 size)
        {
            return new Size4(size.Height, size.Width, size.Depth, 0);
        }
        public static explicit operator Size3(Size4 size) => Unsafe.As<Size4, Size3>(ref size);
        public static explicit operator Size2(Size4 size) => Unsafe.As<Size4, Size2>(ref size);
    }
}