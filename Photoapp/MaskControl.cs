using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Photoapp
{

    class MaskControl
    {
        public byte[,] MapRemembered { get; set; } // move boundaries

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

        public void CalcreturnFull(Bitmap newBitmap, bool remove)
        {

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

            // form here on


            //changed from here one for removing meaning if result == 0 it will color blue if result else then it will be transparent
            if (remove)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (result[x, y] == 2)
                        {
                            MapRemembered[x - 1, y - 1] = 0;
       
                        }
                        if(result[x, y] == 1)
                        {
                            if(MapRemembered[x - 1, y - 1] == 0)
                            {
                                MapRemembered[x - 1, y - 1] = 0;
                            }
                            else
                            {
                                MapRemembered[x - 1, y - 1] = 1;
                            }
                           
               
                        }
                    }
                }
        
            }
            else
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                
                        if (result[x, y] == 1)
                        {

                            if(MapRemembered[x - 1, y - 1] == 2)
                            {
                                MapRemembered[x - 1, y - 1] = 2;
                            }
                            else
                            {
                                MapRemembered[x - 1, y - 1] = 1;
                            }
                         

                           
                            
                        
                        }
                        else if (result[x, y] == 2)
                        {
                            MapRemembered[x - 1, y - 1] = 2;
                        }
                    }
                }
            }
       

            // Merge the processed bitmap with the old bitmap

        }

        public void MergeAndClearEdges(Bitmap newBitmap, Color fillColor)
        {
            CalcreturnFull(newBitmap,false);
        }
        public void MergeAndRemove(Bitmap newBitmap, Color fillColor)
        {
            CalcreturnFull(newBitmap,true);
        }
    }
}