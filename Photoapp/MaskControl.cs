using System;
using System.Collections.Generic;
using System.Drawing;

namespace Photoapp
{

    class MaskControl
    {
        public int invert(int max, int value, int min)
        {
            return (max - value + min);
        }

        public static int[,] FloodFill(int[,] image, int sr, int sc, int newColor)
        {
            if (image == null || image.Length == 0)
            {
                return image;
            }

            int oldColor = image[sr, sc];
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

                int[] dx = {
          0,
          0,
          1,
          -1
        };
                int[] dy = {
          1,
          -1,
          0,
          0
        };

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

        public Bitmap CalcreturnFull(Bitmap newBitmap)
        {
            if (newBitmap == null)
            {
                return null;
            }
            // make it +1 bigger but do not count it into it on each side meaning +2

            int width = newBitmap.Width + 2;
            int height = newBitmap.Height + 2;

            int[,] imageColors = new int[width, height];
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    Color pixelColor = newBitmap.GetPixel(x - 1, y - 1);
                    if (pixelColor.A == 0)
                    {
                        imageColors[x, y] = 0;
                    }
                    else
                    {
                        imageColors[x, y] = 1;
                    }
                }
            }

            int[,] result = FloodFill(imageColors, 0, 0, 2);

            // invert
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[x, y] = invert(2, result[x, y], 0);
                }
            }

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (result[x, y] == 0)
                    {
                        newBitmap.SetPixel(x - 1, y - 1, Color.Transparent);
                    }
                    else if (result[x, y] == 1) // for now also blue
                    {
                        newBitmap.SetPixel(x - 1, y - 1, Color.Blue);
                    }
                    else
                    {
                        newBitmap.SetPixel(x - 1, y - 1, Color.Blue);
                    }

                }
            }

            return newBitmap;
        }
        // If I was to have a matrix the size of the line and a bit bigger I can check if neighboring are now more given the line keeps the same width
        public Bitmap MergeAndClearEdges(Bitmap newBitmap, Bitmap oldBitmap, Color fillColor)
        {
            if (newBitmap.Width != oldBitmap.Width || newBitmap.Height != oldBitmap.Height)
                throw new ArgumentException("Bitmaps must be of the same dimensions");

            Bitmap result = new Bitmap(oldBitmap);
            string filePath = "result_bitmap.png";
            newBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            result = CalcreturnFull(newBitmap);

            return result;
        }

    }
}