<<<<<<< HEAD
﻿using System;
=======
﻿/*
 * Copyright (c) <2011> <by Xalcon @ mmowned.com-Forum>
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

// most of the algorithms and data used in this Class-file has been ported from LibSquish!
// http://code.google.com/p/libsquish/

using System;
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SereniaBLPLib
{
    public class DXTDecompression
    {
        public enum DXTFlags : int
        {
            DXT1 = 1 << 0,
            DXT3 = 1 << 1,
            DXT5 = 1 << 2,
<<<<<<< HEAD
=======
            // Additional Enums not implemented :o
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
        }

        private static void decompress(ref byte[] rgba, byte[] block, int flags)
        {
<<<<<<< HEAD
            byte[] colourBlock = new byte[8];
            byte[] alphaBlock = block;

            if ((flags & ((int)DXTFlags.DXT3 | (int)DXTFlags.DXT5)) != 0)
            {
                Array.Copy(block, 8, colourBlock, 0, 8);
            }

            else
            {
                Array.Copy(block, 0, colourBlock, 0, 8);
            }

            decompressColor(ref rgba, colourBlock, ((flags & (int)DXTFlags.DXT1) != 0));

            if ((flags & (int)DXTFlags.DXT3) != 0)
            {
                DecompressAlphaDxt3(ref rgba, alphaBlock);
            }

            else if ((flags & (int)DXTFlags.DXT5) != 0)
            {
                DecompressAlphaDxt5(ref rgba, alphaBlock);
            }
=======
            // get the block locations
            byte[] colourBlock = new byte[8];
            byte[] alphaBlock = block;
            if ((flags & ((int)DXTFlags.DXT3 | (int)DXTFlags.DXT5)) != 0)
                Array.Copy(block, 8, colourBlock, 0, 8);
            else
                Array.Copy(block, 0, colourBlock, 0, 8);

            // decompress color
            decompressColor(ref rgba, colourBlock, ((flags & (int)DXTFlags.DXT1) != 0));

            // decompress alpha separately if necessary
            if ((flags & (int)DXTFlags.DXT3) != 0)
                DecompressAlphaDxt3(ref rgba, alphaBlock);
            else if ((flags & (int)DXTFlags.DXT5) != 0)
                DecompressAlphaDxt5(ref rgba, alphaBlock);

>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
        }

        private static void DecompressAlphaDxt3(ref byte[] rgba, byte[] block)
        {
            byte[] bytes = block;

<<<<<<< HEAD
            for (int i = 0; i < 8; i++)
            {
                byte quant = bytes[i];
                byte lo = (byte)(quant & 0x0F);
                byte hi = (byte)(quant & 0xF0);

=======
            // Unpack the alpha values pairwise
            for (int i = 0; i < 8; i++)
            {
                // Quantise down to 4 bits
                byte quant = bytes[i];

                byte lo = (byte)(quant & 0x0F);
                byte hi = (byte)(quant & 0xF0);

                // Convert back up to bytes
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                rgba[8 * i + 3] = (byte)(lo | (lo << 4));
                rgba[8 * i + 7] = (byte)(hi | (hi >> 4));
            }
        }

        private static void DecompressAlphaDxt5(ref byte[] rgba, byte[] block)
        {
<<<<<<< HEAD
            byte[] bytes = block;

            int alpha0 = bytes[0];
            int alpha1 = bytes[1];

            byte[] codes = new byte[8];

            codes[0] = (byte)alpha0;
            codes[1] = (byte)alpha1;

            if (alpha0 <= alpha1)
            {
                for (int i = 1; i < 5; i++)
                {
                    codes[1 + i] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
                }

                codes[6] = 0;
                codes[7] = 255;
            }

            else
            {
=======
            // Get the two alpha values
            byte[] bytes = block;
            int alpha0 = bytes[0];
            int alpha1 = bytes[1];

            // compare the values to build the codebook
            byte[] codes = new byte[8];
            codes[0] = (byte)alpha0;
            codes[1] = (byte)alpha1;
            if (alpha0 <= alpha1)
            {
                // Use 5-Alpha Codebook
                for (int i = 1; i < 5; i++)
                    codes[1 + i] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
                codes[6] = 0;
                codes[7] = 255;
            }
            else
            {
                // Use 7-Alpha Codebook
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                for (int i = 1; i < 7; i++)
                {
                    codes[i + 1] = (byte)(((7 - i) * alpha0 + i * alpha1) / 7);
                }
            }

<<<<<<< HEAD
            byte[] indices = new byte[16];
            byte[] blockSrc = bytes;

            int blockSrc_pos = 2;

            byte[] dest = indices;

            int indices_pos = 0;

            for (int i = 0; i < 2; i++)
            {
                int value = 0;

                for (int j = 0; j < 3; j++)
                {
                    int _byte = blockSrc[blockSrc_pos++];

                    value |= (_byte << 8 * j);
                }

                for (int j = 0; j < 8; j++)
                {
                    int index = (value >> 3 * j) & 0x07;

=======
            // decode indices
            byte[] indices = new byte[16];
            byte[] blockSrc = bytes;
            int blockSrc_pos = 2;
            byte[] dest = indices;
            int indices_pos = 0;
            for (int i = 0; i < 2; i++)
            {
                // grab 3 bytes
                int value = 0;
                for (int j = 0; j < 3; j++)
                {
                    int _byte = blockSrc[blockSrc_pos++];
                    value |= (_byte << 8 * j);
                }

                // unpack 8 3-bit values from it
                for (int j = 0; j < 8; j++)
                {
                    int index = (value >> 3 * j) & 0x07;
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                    dest[indices_pos++] = (byte)index;
                }
            }

<<<<<<< HEAD
=======
            // write out the indexed coebook values
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            for (int i = 0; i < 16; i++)
            {
                rgba[4 * i + 3] = codes[indices[i]];
            }
        }

        private static void decompressColor(ref byte[] rgba, byte[] block, bool isDxt1)
        {
            byte[] bytes = block;
<<<<<<< HEAD
            byte[] codes = new byte[16];

            int a = unpack565(bytes, 0, ref codes, 0);
            int b = unpack565(bytes, 2, ref codes, 4);

=======

            // Unpack Endpoints
            byte[] codes = new byte[16];
            int a = unpack565(bytes, 0, ref codes, 0);
            int b = unpack565(bytes, 2, ref codes, 4);

            // generate Midpoints
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            for (int i = 0; i < 3; i++)
            {
                int c = codes[i];
                int d = codes[4 + i];

                if (isDxt1 && a <= b)
                {
                    codes[8 + i] = (byte)((c + d) / 2);
                    codes[12 + i] = 0;
                }
<<<<<<< HEAD

=======
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                else
                {
                    codes[8 + i] = (byte)((2 * c + d) / 3);
                    codes[12 + i] = (byte)((c + 2 * d) / 3);
                }
            }

<<<<<<< HEAD
            codes[8 + 3] = 255;
            codes[12 + 3] = (isDxt1 && a <= b) ? (byte)0 : (byte)255;

            byte[] indices = new byte[16];

=======
            // Fill in alpha for intermediate values
            codes[8 + 3] = 255;
            codes[12 + 3] = (isDxt1 && a <= b) ? (byte)0 : (byte)255;

            //unpack the indices
            byte[] indices = new byte[16];
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            for (int i = 0; i < 4; i++)
            {
                byte packed = bytes[4 + i];

                indices[0 + i * 4] = (byte)(packed & 0x3);
                indices[1 + i * 4] = (byte)((packed >> 2) & 0x3);
                indices[2 + i * 4] = (byte)((packed >> 4) & 0x3);
                indices[3 + i * 4] = (byte)((packed >> 6) & 0x3);
            }

<<<<<<< HEAD
            for (int i = 0; i < 16; i++)
            {
                byte offset = (byte)(4 * indices[i]);

=======
            // store out the colours
            for (int i = 0; i < 16; i++)
            {
                byte offset = (byte)(4 * indices[i]);
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                for (int j = 0; j < 4; j++)
                {
                    rgba[4 * i + j] = codes[offset + j];
                }
            }
        }

        private static int unpack565(byte[] packed, int packed_offset, ref byte[] colour, int colour_offset)
        {
<<<<<<< HEAD
            int value = (int)packed[0 + packed_offset] | ((int)packed[1 + packed_offset] << 8);

=======
            // Build packed value
            int value = (int)packed[0 + packed_offset] | ((int)packed[1 + packed_offset] << 8);

            // get components in the stored range
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            byte red = (byte)((value >> 11) & 0x1F);
            byte green = (byte)((value >> 5) & 0x3F);
            byte blue = (byte)(value & 0x1F);

<<<<<<< HEAD
=======
            // Scale up to 8 Bit
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            colour[0 + colour_offset] = (byte)((red << 3) | (red >> 2));
            colour[1 + colour_offset] = (byte)((green << 2) | (green >> 4));
            colour[2 + colour_offset] = (byte)((blue << 3) | (blue >> 2));
            colour[3 + colour_offset] = 255;

            return value;
        }

        public static void decompressImage(out byte[] rgba, int width, int height, byte[] blocks, int flags)
        {
            rgba = new byte[width * height * 4];

<<<<<<< HEAD
            byte[] sourceBlock = blocks;

            int sourceBlock_pos = 0;
            int bytesPerBlock = ((flags & (int)DXTFlags.DXT1) != 0) ? 8 : 16;

=======
            // initialise the block input
            byte[] sourceBlock = blocks;
            int sourceBlock_pos = 0;
            int bytesPerBlock = ((flags & (int)DXTFlags.DXT1) != 0) ? 8 : 16;

            // loop over blocks
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
<<<<<<< HEAD
                    byte[] targetRGBA = new byte[4 * 16];

                    int targetRGBA_pos = 0;

                    byte[] sourceBlockBuffer = new byte[bytesPerBlock];

                    if (sourceBlock.Length == sourceBlock_pos)
                    {
                        continue;
                    }

                    Array.Copy(sourceBlock, sourceBlock_pos, sourceBlockBuffer, 0, bytesPerBlock);

                    decompress(ref targetRGBA, sourceBlockBuffer, flags);

=======
                    // decompress the block
                    byte[] targetRGBA = new byte[4 * 16];
                    int targetRGBA_pos = 0;
                    byte[] sourceBlockBuffer = new byte[bytesPerBlock]; // größe korrekt?
                    if (sourceBlock.Length == sourceBlock_pos) continue;
                    Array.Copy(sourceBlock, sourceBlock_pos, sourceBlockBuffer, 0, bytesPerBlock);
                    //sourceBlock.CopyTo(sourceBlockBuffer, sourceBlock_pos);
                    decompress(ref targetRGBA, sourceBlockBuffer, flags);

                    // Write the decompressed pixels to the correct image locations
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                    byte[] sourcePixel = new byte[4];

                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int sx = x + px;
                            int sy = y + py;
<<<<<<< HEAD

=======
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                            if (sx < width && sy < height)
                            {
                                int targetPixel = 4 * (width * sy + sx);

<<<<<<< HEAD
                                Array.Copy(targetRGBA, targetRGBA_pos, sourcePixel, 0, 4);

                                targetRGBA_pos += 4;

                                for (int i = 0; i < 4; i++)
                                {
                                    rgba[targetPixel + i] = sourcePixel[i];
                                }
                            }

                            else
                            {
=======
                                //targetRGBA.CopyTo(sourcePixel, targetRGBA_pos);
                                Array.Copy(targetRGBA, targetRGBA_pos, sourcePixel, 0, 4);
                                targetRGBA_pos += 4;

                                for (int i = 0; i < 4; i++)
                                    rgba[targetPixel + i] = sourcePixel[i];
                            }
                            else
                            {
                                // Ignore that pixel
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                                targetRGBA_pos += 4;
                            }
                        }
                    }
<<<<<<< HEAD

=======
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
                    sourceBlock_pos += bytesPerBlock;
                }
            }
        }
<<<<<<< HEAD
    };
};
=======
    }
}
>>>>>>> eaaffc5dda6f2e25a5c5384e6e0e8fbabf4c1c21
