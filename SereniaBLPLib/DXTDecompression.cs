using System;

namespace SereniaBLPLib
{
    public class DXTDecompression
    {
        public enum DXTFlags : int
        {
            DXT1 = 1 << 0,
            DXT3 = 1 << 1,
            DXT5 = 1 << 2,
        }

        public static void decompressImage(out byte[] rgba, int width, int height, byte[] blocks, int flags)
        {
            rgba = new byte[width * height * 4];
            if (blocks == null || width <= 0 || height <= 0)
                return;

            var isDxt1 = (flags & (int)DXTFlags.DXT1) != 0;
            var isDxt3 = (flags & (int)DXTFlags.DXT3) != 0;
            var isDxt5 = (flags & (int)DXTFlags.DXT5) != 0;
            var bytesPerBlock = isDxt1 ? 8 : 16;
            var colourOffset = isDxt1 ? 0 : 8;
            var sourcePos = 0;

            Span<byte> block = stackalloc byte[64];

            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    if (sourcePos + bytesPerBlock > blocks.Length)
                        return;

                    var source = new ReadOnlySpan<byte>(blocks, sourcePos, bytesPerBlock);
                    sourcePos += bytesPerBlock;

                    DecompressColour(block, source.Slice(colourOffset, 8), isDxt1);
                    if (isDxt3)
                        DecompressAlphaDxt3(block, source);
                    else if (isDxt5)
                        DecompressAlphaDxt5(block, source);

                    var rows = Math.Min(4, height - y);
                    var columns = Math.Min(4, width - x);
                    for (var py = 0; py < rows; ++py)
                    {
                        var destination = 4 * (width * (y + py) + x);
                        block.Slice(16 * py, columns * 4).CopyTo(new Span<byte>(rgba, destination, columns * 4));
                    }
                }
            }
        }

        private static void DecompressColour(Span<byte> rgba, ReadOnlySpan<byte> block, bool isDxt1)
        {
            Span<byte> codes = stackalloc byte[16];

            var a = Unpack565(block, 0, codes, 0);
            var b = Unpack565(block, 2, codes, 4);
            var thirdMode = isDxt1 && a <= b;

            for (var i = 0; i < 3; ++i)
            {
                int c = codes[i];
                int d = codes[4 + i];
                if (thirdMode)
                {
                    codes[8 + i] = (byte)((c + d) / 2);
                    codes[12 + i] = 0;
                }
                else
                {
                    codes[8 + i] = (byte)((2 * c + d) / 3);
                    codes[12 + i] = (byte)((c + 2 * d) / 3);
                }
            }

            codes[11] = 255;
            codes[15] = thirdMode ? (byte)0 : (byte)255;

            for (var i = 0; i < 4; ++i)
            {
                int packed = block[4 + i];
                for (var j = 0; j < 4; ++j)
                {
                    var offset = 4 * ((packed >> (2 * j)) & 0x3);
                    var target = 16 * i + 4 * j;
                    rgba[target] = codes[offset];
                    rgba[target + 1] = codes[offset + 1];
                    rgba[target + 2] = codes[offset + 2];
                    rgba[target + 3] = codes[offset + 3];
                }
            }
        }

        private static void DecompressAlphaDxt3(Span<byte> rgba, ReadOnlySpan<byte> block)
        {
            for (var i = 0; i < 8; ++i)
            {
                int quant = block[i];
                var lo = (byte)(quant & 0x0F);
                var hi = (byte)(quant & 0xF0);
                rgba[8 * i + 3] = (byte)(lo | (lo << 4));
                rgba[8 * i + 7] = (byte)(hi | (hi >> 4));
            }
        }

        private static void DecompressAlphaDxt5(Span<byte> rgba, ReadOnlySpan<byte> block)
        {
            int alpha0 = block[0];
            int alpha1 = block[1];

            Span<byte> codes = stackalloc byte[8];
            codes[0] = (byte)alpha0;
            codes[1] = (byte)alpha1;
            if (alpha0 <= alpha1)
            {
                for (var i = 1; i < 5; ++i)
                    codes[1 + i] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
                codes[6] = 0;
                codes[7] = 255;
            }
            else
            {
                for (var i = 1; i < 7; ++i)
                    codes[i + 1] = (byte)(((7 - i) * alpha0 + i * alpha1) / 7);
            }

            var pixel = 0;
            for (var i = 0; i < 2; ++i)
            {
                var value = 0;
                for (var j = 0; j < 3; ++j)
                    value |= block[2 + i * 3 + j] << (8 * j);

                for (var j = 0; j < 8; ++j, ++pixel)
                    rgba[4 * pixel + 3] = codes[(value >> (3 * j)) & 0x07];
            }
        }

        private static int Unpack565(ReadOnlySpan<byte> packed, int packedOffset, Span<byte> colour, int colourOffset)
        {
            var value = packed[packedOffset] | (packed[packedOffset + 1] << 8);

            var red = (byte)((value >> 11) & 0x1F);
            var green = (byte)((value >> 5) & 0x3F);
            var blue = (byte)(value & 0x1F);

            colour[colourOffset] = (byte)((red << 3) | (red >> 2));
            colour[colourOffset + 1] = (byte)((green << 2) | (green >> 4));
            colour[colourOffset + 2] = (byte)((blue << 3) | (blue >> 2));
            colour[colourOffset + 3] = 255;

            return value;
        }
    }
}
