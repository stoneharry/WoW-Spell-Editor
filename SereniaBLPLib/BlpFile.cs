/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace SereniaBLPLib
{
    // Some Helper Struct to store Color-Data
    public struct ARGBColor8
    {
        public byte red;
        public byte green;
        public byte blue;
        public byte alpha;

        public ARGBColor8(int r, int g, int b)
        {
            this.red = (byte)r;
            this.green = (byte)g;
            this.blue = (byte)b;
            this.alpha = (byte)255;
        }

        public ARGBColor8(byte r, byte g, byte b)
        {
            this.red = r;
            this.green = g;
            this.blue = b;
            this.alpha = 255;
        }

        public ARGBColor8(int a, int r, int g, int b)
        {
            this.red = (byte)r;
            this.green = (byte)g;
            this.blue = (byte)b;
            this.alpha = (byte)a;
        }

        public ARGBColor8(byte a, byte r, byte g, byte b)
        {
            this.red = r;
            this.green = g;
            this.blue = b;
            this.alpha = a;
        }

        /// <summary>
        /// Converts the given Pixel-Array into the BGRA-Format
        /// This will also work vice versa
        /// </summary>
        /// <param name="pixel"></param>
        public static void convertToBGRA(ref byte[] pixel)
        {
            byte tmp = 0;
            for (int i = 0; i < pixel.Length; i += 4)
            {
                tmp = pixel[i]; // store red
                pixel[i] = pixel[i + 2]; // Write blue into red
                pixel[i + 2] = tmp; // write stored red into blue
            }
        }
    }

    public class BlpFile : IDisposable
    {
        #region Private Fields
        uint type; // compression: 0 = JPEG Compression, 1 = Uncompressed or DirectX Compression
        byte encoding; // 1 = Uncompressed, 2 = DirectX Compressed
        byte alphaDepth; // 0 = no alpha, 1 = 1 Bit, 4 = Bit (only DXT3), 8 = 8 Bit Alpha
        byte alphaEncoding; // 0: DXT1 alpha (0 or 1 Bit alpha), 1 = DXT2/3 alpha (4 Bit), 7: DXT4/5 (interpolated alpha)
        byte hasMipmaps; // If true (1), then there are Mipmaps
        int width; // X Resolution of the biggest Mipmap
        int height; // Y Resolution of the biggest Mipmap

        uint[] mipmapOffsets = new uint[16]; // Offset for every Mipmap level. If 0 = no more mitmap level
        uint[] mippmapSize = new uint[16]; // Size for every level
        ARGBColor8[] paletteBGRA = new ARGBColor8[256]; // The color-palette for non-compressed pictures

        Stream str; // Reference of the stream
        #endregion

        #region Private Methods
        /// <summary>
        /// Extracts the palettized Image-Data from the given Mipmap and returns a byte-Array in the 32Bit RGBA-Format
        /// </summary>
        /// <param name="mipmap">The desired Mipmap-Level. If the given level is invalid, the smallest available level is choosen</param>
        /// <returns>Pixel-data</returns>
        private byte[] getPictureUncompressedByteArray(int MipmapLevel)
        {
            if (MipmapLevel >= this.MipMapCount) MipmapLevel = this.MipMapCount - 1;
            if (MipmapLevel < 0) MipmapLevel = 0;
            byte[] pic = new byte[((this.width * this.height) * 4) / (int)(Math.Pow(2, MipmapLevel))];
            byte[] indices = this.getPictureData(MipmapLevel);
            for (int i = 0; i < indices.Length; i++)
            {
                pic[i * 4] = this.paletteBGRA[indices[i]].red;
                pic[i * 4 + 1] = this.paletteBGRA[indices[i]].green;
                pic[i * 4 + 2] = this.paletteBGRA[indices[i]].blue;
                pic[i * 4 + 3] = (this.alphaDepth > 0) ? this.paletteBGRA[indices[i]].alpha : (byte)255;
            }
            return pic;
        }

        /// <summary>
        /// Returns the raw Mipmap-Image Data. This data can either be compressed or uncompressed, depending on the Header-Data
        /// </summary>
        /// <param name="MipmapLevel"></param>
        /// <returns></returns>
        private byte[] getPictureData(int MipmapLevel)
        {
            if (this.str != null)
            {
                byte[] data;
                if (MipmapLevel >= this.MipMapCount) MipmapLevel = this.MipMapCount - 1;
                if (MipmapLevel < 0) MipmapLevel = 0;

                data = new byte[this.mippmapSize[MipmapLevel]];
                this.str.Position = (int)this.mipmapOffsets[MipmapLevel];
                this.str.Read(data, 0, data.Length);
                return data;
            }
            return null;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Returns the amount of Mipmaps in this BLP-File
        /// </summary>
        public int MipMapCount
        {
            get
            {
                int i = 0;
                while (this.mipmapOffsets[i] != 0) i++;
                return i;
            }
        }
        #endregion

        #region public Methods
        public BlpFile(Stream _str)
        {
            this.str = _str;
            byte[] buffer = new byte[4];
            // Well, have to fix this... looks weird o.O
            this.str.Read(buffer, 0, 4);

            // Checking for correct Magic-Code
            if ((new ASCIIEncoding()).GetString(buffer) != "BLP2")
                throw new Exception("Invalid BLP Format");

            // Reading type
            str.Read(buffer, 0, 4);
            this.type = BitConverter.ToUInt32(buffer, 0);
            if (this.type != 1)
                throw new Exception("Invalid BLP-Type! Should be 1 but " + this.type + " was found");

            // Reading encoding, alphaBitDepth, alphaEncoding and hasMipmaps
            this.str.Read(buffer, 0, 4);
            this.encoding = buffer[0];
            this.alphaDepth = buffer[1];
            this.alphaEncoding = buffer[2];
            this.hasMipmaps = buffer[3];

            // Reading width
            str.Read(buffer, 0, 4);
            this.width = BitConverter.ToInt32(buffer, 0);

            // Reading height
            str.Read(buffer, 0, 4);
            this.height = BitConverter.ToInt32(buffer, 0);

            // Reading MipmapOffset Array
            for (int i = 0; i < 16; i++)
            {
                _str.Read(buffer, 0, 4);
                this.mipmapOffsets[i] = BitConverter.ToUInt32(buffer, 0);
            }

            // Reading MipmapSize Array
            for (int i = 0; i < 16; i++)
            {
                str.Read(buffer, 0, 4);
                this.mippmapSize[i] = BitConverter.ToUInt32(buffer, 0);
            }

            // When encoding is 1, there is no image compression and we have to read a color palette
            if (this.encoding == 1)
            {
                // Reading palette
                for (int i = 0; i < 256; i++)
                {
                    byte[] color = new byte[4];
                    str.Read(color, 0, 4);
                    this.paletteBGRA[i].blue = color[0];
                    this.paletteBGRA[i].green = color[1];
                    this.paletteBGRA[i].red = color[2];
                    this.paletteBGRA[i].alpha = color[3];
                }
            }
        }

        /// <summary>
        /// Returns the uncompressed image as a bytarray in the 32pppRGBA-Format
        /// </summary>
        /// <param name="MipmapLevel">The desired Mipmap-Level. If the given level is invalid, the smallest available level is choosen</param>
        /// <returns></returns>
        public byte[] getImageBytes(int MipmapLevel)
        {
            byte[] pic;

            if (this.encoding == 2)
            {
                // Determine the correct DXT-Format
                int flag = (this.alphaDepth > 1) ? ((this.alphaEncoding == 7) ? (int)DXTDecompression.DXTFlags.DXT5 : (int)DXTDecompression.DXTFlags.DXT3) : (int)DXTDecompression.DXTFlags.DXT1;
                // Decompress the picture
                DXTDecompression.decompressImage(out pic, (this.width / (int)(Math.Pow(2, MipmapLevel))), (this.height / (int)(Math.Pow(2, MipmapLevel))), this.getPictureData(MipmapLevel), flag);
            }
            else
            {
                // Using the palette to determine the color
                pic = this.getPictureUncompressedByteArray(MipmapLevel);
            }

            return pic;
        }

        /// <summary>
        /// Converts the BLP to a System.Drawing.Bitmap
        /// </summary>
        /// <param name="MipmapLevel">The desired Mipmap-Level. If the given level is invalid, the smallest available level is choosen</param>
        /// <returns>The Bitmap</returns>
        public Bitmap getBitmap(int MipmapLevel)
        {
            int x = (this.width / (int)(Math.Pow(2, MipmapLevel))), y = (this.height / (int)(Math.Pow(2, MipmapLevel)));
            Bitmap bmp = new Bitmap(x, y);
            byte[] pic = getImageBytes(MipmapLevel); // This bytearray stores the Pixel-Data

            // Faster bitmap Data copy
            System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, x, y), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // when we want to copy the pixeldata directly into the bitmap, we have to convert them into BGRA befor doing so
            ARGBColor8.convertToBGRA(ref pic);
            System.Runtime.InteropServices.Marshal.Copy(pic, 0, bmpdata.Scan0, pic.Length); // copy! :D
            bmp.UnlockBits(bmpdata);

            /*
            // Pushing everything into the Bitmap
            // This technique is realy slow and should not be used
            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    int r, g, b, a;
                    r = pic[(y * this.width + x) * 4 + 0];
                    g = pic[(y * this.width + x) * 4 + 1];
                    b = pic[(y * this.width + x) * 4 + 2];
                    a = pic[(y * this.width + x) * 4 + 3];
                    Color col = Color.FromArgb(a, r, g, b);
                    bmp.SetPixel(x, y, col);
                }
            }
            */

            return bmp;
        }

        /// <summary>
        /// Runs close()
        /// </summary>
        public void Dispose()
        {
            this.close();
        }

        /// <summary>
        /// Closes the Memorystream
        /// </summary>
        public void close()
        {
            if (this.str != null)
            {
                this.str.Close();
                this.str = null;
            }
        }
        #endregion
    }
}
