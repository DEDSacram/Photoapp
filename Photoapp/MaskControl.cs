using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Photoapp
{

    class MaskControl
    {
        public byte invert(byte max, byte value, byte min)
        {
            return (byte)(max - value + min);
        }

        public static byte[,] FloodFill(byte[,] image, int sr, int sc, byte newColor)
        {
            if (image == null || image.Length == 0)
            {
                return image;
            }

            byte oldColor = image[sr, sc];
            if (oldColor == newColor)
            {
                return image;
            }

            int rows = image.GetLength(0);
            int cols = image.GetLength(1);
            Queue<(int, int)> queue = new Queue<(int, int)>();
            queue.Enqueue((sr, sc));
            image[sr, sc] = newColor;

            while (queue.Count > 0)
            {
                (int x, int y) = queue.Dequeue();

                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx >= 0 && nx < rows && ny >= 0 && ny < cols && image[nx, ny] == oldColor)
                    {
                        image[nx, ny] = newColor;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return image;
        }

        public Bitmap CalcreturnFull(Bitmap newBitmap,Bitmap oldBitmap, bool remove)
        {
            if (newBitmap == null)
            {
                return null;
            }

            int width = newBitmap.Width + 2;
            int height = newBitmap.Height + 2;

            byte[,] imageColors = new byte[width, height];
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    Color pixelColor = newBitmap.GetPixel(x - 1, y - 1);
                    imageColors[x, y] = (pixelColor.A == 0) ? (byte)0 : (byte)1;
                }
            }

            byte[,] result = FloodFill(imageColors, 0, 0, 2);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = invert(2, result[x, y], 0);
                    
                }
            }


            //changed from here one for removing meaning if result == 0 it will color blue if result else then it will be transparent
            if (remove)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (result[x, y] != 0)
                        {
                      
                            oldBitmap.SetPixel(x - 1, y - 1, Color.Transparent);
                        }
                    }
                }
                return oldBitmap;
            }
            else
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (result[x, y] == 0)
                        {
                            newBitmap.SetPixel(x - 1, y - 1, Color.Transparent);
                        }
                        else
                        {
                            newBitmap.SetPixel(x - 1, y - 1, Color.Blue);
                        }
                    }
                }
            }
       

            // Merge the processed bitmap with the old bitmap
            using (Graphics g = Graphics.FromImage(oldBitmap))
            {
                g.CompositingMode = CompositingMode.SourceOver; // Ensure transparency blending
                g.DrawImage(newBitmap, 0, 0); // Draw the processed bitmap onto the result bitmap
            }


            return oldBitmap;
        }

        public Bitmap MergeAndClearEdges(Bitmap newBitmap, Bitmap oldBitmap, Color fillColor)
        {
            if (newBitmap.Width != oldBitmap.Width || newBitmap.Height != oldBitmap.Height)
                throw new ArgumentException("Bitmaps must be of the same dimensions");
            // Apply the CalcreturnFull method to the new bitmap
            Bitmap processedBitmap = CalcreturnFull(newBitmap,oldBitmap,false);

            return processedBitmap;
        }
        public Bitmap MergeAndRemove(Bitmap newBitmap, Bitmap oldBitmap, Color fillColor)
        {
            if (newBitmap.Width != oldBitmap.Width || newBitmap.Height != oldBitmap.Height)
                throw new ArgumentException("Bitmaps must be of the same dimensions");
            // Apply the CalcreturnFull method to the new bitmap
            Bitmap processedBitmap = CalcreturnFull(newBitmap,oldBitmap,true);

 
            return processedBitmap;
        }
    }
}