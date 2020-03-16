/*

    Copyright © Charles Reilly 2020

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;

namespace img2beeb
{
    class Program
    {
        static byte[] mode2ColourTable = new byte[] {
            0x00, 0x03, 0x0C, 0x0F,
            0x30, 0x33, 0x3C, 0x3F,
            0xC0, 0xC3, 0xCC, 0xCF,
            0xF0, 0xF3, 0xFC, 0xFF,
        };

        private static void ConvertGifToMode2(string sourceFile, string outputPath)
        {
            string newPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(sourceFile));
            if (File.Exists(newPath))
                return;

            DateTime modificationTime = File.GetLastWriteTimeUtc(sourceFile);

            Image<Rgba32> gif = Image.Load<Rgba32>(sourceFile);

            int height = 256;
            int width = 160;
            int frameCount = gif.Frames.Count;
            // Currently use ints to hold a list of pixel colours for a pixel.  Each takes three bits,
            // so can only do 10.  Easily extended with a long though.
            Debug.Assert(frameCount <= 10);
            if (frameCount > 10)
            {
                throw new ApplicationException("The source image has more than 10 frames");
            }

            // These contains every observed pattern of colour cycling
            HashSet<int> staticCycles = new HashSet<int>(); // Cycles where the colour doesn't change
            HashSet<int> colourCycles = new HashSet<int>(); // Cycles where the colour changes

            byte[, ,] frames = new byte[frameCount, width, height];

            int frameIndex = 0;
            foreach (ImageFrame<Rgba32> frame in gif.Frames)
            {
                Debug.Assert(frame.Width == width * 4);
                Debug.Assert(frame.Height == height * 2);
                if (frame.Width != width * 4)
                {
                    throw new ApplicationException(string.Format("The source image is not {0} pixels wide", width * 4));
                }
                if (frame.Height != height * 2)
                {
                    throw new ApplicationException(string.Format("The source image is not {0} pixels high", height * 2));
                }

                for (int y = 0; y != height; ++y)
                {
                    for (int x = 0; x != width; ++x)
                    {
                        int xs = 4 * x;
                        int ys = 2 * y;
                        Color c = frame[xs, ys];
                        for (int y0 = 0; y0 != 2; ++y0)
                        {
                            for (int x0 = 0; x0 != 4; ++x0)
                            {
                                Color c2 = frame[xs + x0, ys + y0];
                                Debug.Assert(c == c2);
                            }
                        }
                        Rgba32 p = c.ToPixel<Rgba32>();
                        frames[frameIndex, x, y] = (byte)BeebColour(128, p.R, p.G, p.B);
                    }
                }
                ++frameIndex;
            }

            // Calculate vector of pixel colours across frames;
            // remember each unique vector.
            for (int y = 0; y != height; ++y)
            {
                for (int x = 0; x != width; ++x)
                {
                    byte colour = frames[0, x, y];
                    int vector = colour;
                    bool match = true;
                    for (int frame = 1; frame != frameCount; ++frame)
                    {
                        byte current = frames[frame, x, y];
                        vector = (vector << 3) | current;
                        match = match && (colour == current);
                    }
                    if (match)
                        staticCycles.Add(vector);
                    else
                        colourCycles.Add(vector);
                }
            }

            Debug.Assert(staticCycles.Count + colourCycles.Count <= 16);
            if (staticCycles.Count + colourCycles.Count > 16)
            {
                throw new ApplicationException("The animation requires more than 16 colours");
            }

            // These indices could be assigned on a single pass through the data,
            // but doing it at the end means they can be sorted.

            List<int> colourCyclesSorted = colourCycles.ToList();
            colourCyclesSorted.Sort();
            List<int> staticCyclesSorted = staticCycles.ToList();
            staticCyclesSorted.Sort();

            Dictionary<int, byte> colourNumbers = new Dictionary<int, byte>();
            foreach (int cycle in staticCyclesSorted)
            {
                colourNumbers.Add(cycle, (byte)(colourNumbers.Count));
            }
            foreach (int cycle in colourCyclesSorted)
            {
                colourNumbers.Add(cycle, (byte)(colourNumbers.Count));
            }

            byte[,] final = new byte[width, height];
            for (int y = 0; y != height; ++y)
            {
                for (int x = 0; x != width; ++x)
                {
                    // Create vector of observed colours across frames
                    int vector = 0;
                    for (int frame = 0; frame != frameCount; ++frame)
                    {
                        vector = (vector << 3) | frames[frame, x, y];
                    }
                    final[x, y] = colourNumbers[vector];
                }
            }

            int rowCount = (height + 7) / 8;
            int rowLength = 640;

            byte[] mode2 = new byte[rowLength * rowCount];

            for (int y = 0; y != height; ++y)
            {
                int rowStart = (y / 8) * rowLength + (y % 8);
                for (int x = 0; x != width; ++x)
                {
                    // Source pixels are doubled up so each byte has two pixels the same colour
                    int sourcePixel = final[x, y];
                    Debug.Assert(sourcePixel < 16);
                    byte mask = (x % 2) == 0 ? (byte)0xAA : (byte)0x55;
                    byte colour = (byte)(mode2ColourTable[sourcePixel] & mask);
                    int destIndex = rowStart + (x / 2) * 8;
                    mode2[destIndex] |= colour;
                }
            }

            byte[] palette = new byte[256];
            byte[] message = Encoding.ASCII.GetBytes("Mode2 Animation");
            message.CopyTo(palette, 0);

            int index = message.Length + 1;
            palette[index++] = (byte)frameCount;
            palette[index++] = (byte)staticCycles.Count;
            palette[index++] = (byte)colourCycles.Count;
            for (int colour = 0; colour != staticCyclesSorted.Count; ++colour)
            {
                palette[index++] = (byte)(staticCyclesSorted[colour] & 0x07);
            }
            for (int frame = 0; frame != frameCount; ++frame)
            {
                for (int colour = 0; colour != colourCyclesSorted.Count; ++colour)
                {
                    palette[index++] = (byte)((colourCyclesSorted[colour] >> (3 * (frameCount - frame - 1))) & 0x07);
                }
                // Get frame delay in centiseconds
                GifFrameMetadata fmd = gif.Frames[frame].Metadata.GetFormatMetadata(GifFormat.Instance);
                int frameDelay = fmd.FrameDelay;
                // The BASIC program subtracts 2 to account for its own sluggishness
                Debug.Assert(frameDelay >= 3 && frameDelay < 256);
                if (frameDelay < 3 || frameDelay >= 256)
                {
                    throw new ApplicationException("The frame delay is too short (or too long!)");
                }
                palette[index++] = (byte)frameDelay;
            }

            using (FileStream fs = new FileStream(newPath, FileMode.CreateNew, FileAccess.Write))
            {
                fs.Write(palette, 0, palette.Length);
                fs.Write(mode2, 0, mode2.Length);
            }
            File.SetLastWriteTimeUtc(newPath, modificationTime);
        }

        private static int BeebColour(int threshold, int r, int g, int b)
        {
            int c = 0;
            if (r > threshold)
                c |= 1;
            if (g > threshold)
                c |= 2;
            if (b > threshold)
                c |= 4;
            return c;
        }

        private static void ConvertGifFolderToMode2(string source, string outputPath)
        {
            foreach (string sourceFile in Directory.GetFiles(source, "*.gif"))
            {
                Console.WriteLine(sourceFile);
                ConvertGifToMode2(sourceFile, outputPath);
            }
        }

        static void ExceptionalMain(string[] args)
        {
            string cd = Environment.CurrentDirectory;

            ConvertGifFolderToMode2(Path.Combine(cd, "gifs"), Path.Combine(cd, "mode2"));
        }

        static void Main(string[] args)
        {
            try
            {
                ExceptionalMain(args);
            }
            catch (ApplicationException e)
            {
                Console.Error.WriteLine("img2beeb: {0}", e.Message);
            }
        }
    }
}
