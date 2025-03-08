using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Windows.Media.Media3D;

namespace Photoapp
{
    // version and layer control
    public class Layer
    {
        public int LayerId
        {
            get;
            set;
        }
        public Bitmap Bitmap
        {
            get;
            set;
        }
        public int Order
        {
            get;
            set;
        } // Added to track the order of layers

        public Point Offset
        {
            get;
            set;
        }

        public Layer(int layerId, Bitmap bitmap)
        {
            LayerId = layerId;
            Bitmap = bitmap;
            Order = 0; // Default order value
            Offset = new Point(0, 0);
        }
    }
    public class UndoEntry
    {
        public int LayerId
        {
            get;
            private set;
        }
        public byte[] ZippedBitmap
        {
            get;
            private set;
        }
        public bool IsTransform
        {
            get;
            private set;
        }

        public bool overrideBitmap
        {
            get;
            set;
        }
        public int width
        {
            get;
            set;
        }
        public int height
        {
            get;
            set;
        }

        public UndoEntry(int layerId, byte[] zippedBitmap)
        {
            LayerId = layerId;
            ZippedBitmap = zippedBitmap;
            IsTransform = false;
            overrideBitmap = false;
        }
    }
    public class LayerManager
    {
        private List<Layer> layers;
        public Stack<UndoEntry> undoStack;
        private int nextLayerId;

        public LayerManager()
        {
            layers = new List<Layer>();
            undoStack = new Stack<UndoEntry>();
            nextLayerId = 1;
        }

        public int GetNextLayerId()
        {
            return nextLayerId++;
        }
        private Layer GetLayerById(int layerId)
        {
            foreach (Layer layer in layers)
            {
                if (layer.LayerId == layerId)
                    return layer;
            }
            return null;
        }

        public List<Layer> GetLayers()
        {
            return layers;
        }
        public void PrintLayerOrder()
        {
            Console.WriteLine("Layer Order:");
            foreach (var layer in layers)
            {
                Console.WriteLine($"Layer ID: {layer.LayerId}");
            }
        }

        // Add a new layer
        public void AddLayer(int layerId, Bitmap bitmap)
        {
            var newLayer = new Layer(layerId, bitmap);
            layers.Add(newLayer);
            UpdateLayerOrder(); // Ensure correct ordering when a new layer is added
        }

        // Remove a layer by ID
        public void RemoveLayer(int layerId)
        {
            layers.RemoveAll(layer => layer.LayerId == layerId);
            UpdateLayerOrder(); // Update order after removal
        }

        // Update the layer order based on visual stacking or manual manipulation
        private void UpdateLayerOrder()
        {
            // Sort layers based on the 'Order' value (ascending)
            var orderedLayers = layers.OrderBy(layer => layer.Order).ToList();
            layers.Clear();
            layers.AddRange(orderedLayers);
        }

        // Update a layer's bitmap and save an undo state
        public void UpdateLayer(int layerId, Bitmap newBitmap)
        {
            foreach (var layer in layers)
            {
                if (layer.LayerId == layerId)
                {
                    SaveUndoState(layer);
                    layer.Bitmap = newBitmap;
                    break;
                }
            }
        }

        public void SaveToUndoStack(int layerId, int[] diff, bool ovveride, Point Previoussize)
        {

            // Convert int[] to byte[]
            byte[] diffBytes = new byte[diff.Length * 4];
            Buffer.BlockCopy(diff, 0, diffBytes, 0, diffBytes.Length);

            Layer layer = GetLayerById(layerId);

            // Compress the byte array
            byte[] compressedData = Compress(diffBytes);

            // Save the compressed data in an UndoEntry
            UndoEntry entry = new UndoEntry(layerId, compressedData);
            entry.overrideBitmap = ovveride;
            entry.width = Previoussize.X;
            entry.height = Previoussize.Y; // cant do it here this is already with the new coords

            undoStack.Push(entry);
        }
        public void RestoreFromUndoStack()
        {
            if (undoStack.Count == 0)
                return; // No undo available.

            // Get the last undo entry from the stack.

            UndoEntry entry = undoStack.Pop();

            // restore full or differential

            // Retrieve the layer from the private list 'layers' by its ID.
            Layer layer = GetLayerById(entry.LayerId);
            if (layer == null)
                return; // Layer not found, nothing to restore.

            // Decompress the stored zipped difference data.
            // Decompress should return an int[] array with one difference per byte.
            int[] diff = Decompress(entry.ZippedBitmap);

            if (entry.overrideBitmap)
            {
                byte[] ovveridecopy = new byte[diff.Length];
                for (int i = 0; i < diff.Length; i++)
                {
                    ovveridecopy[i] = (byte)diff[i];
                }
                try
                {

                    layer.Bitmap = CreateBitmapFromBytes(ovveridecopy, entry.width, entry.height, PixelFormat.Format32bppArgb);
                }
                catch
                {
                    Console.WriteLine(diff.Length);
                    Console.WriteLine(ovveridecopy.Length);
                    Console.WriteLine(layer.Bitmap.Width);
                    Console.WriteLine(entry.width);
                }

                return;
            }

            byte[] diffForDisplay = new byte[diff.Length];
            for (int i = 0; i < diff.Length; i++)
            {
                int shifted = diff[i] + 128;
                if (shifted < 0) shifted = 0;
                if (shifted > 255) shifted = 255;
                diffForDisplay[i] = (byte)shifted;
            }
            byte[] currentImage = GetBytesFromBitmap(layer.Bitmap);

            byte[] reconstructedData = new byte[diff.Length];
            for (int i = 0; i < currentImage.Length; i++)
            {
                int value = currentImage[i] + diff[i];
                if (value < 0) value = 0;
                if (value > 255) value = 255;
                reconstructedData[i] = (byte)value;
            }
            layer.Bitmap = CreateBitmapFromBytes(reconstructedData, layer.Bitmap.Width, layer.Bitmap.Height, PixelFormat.Format32bppArgb);
        }

        // Helper: Extract raw bytes from a Bitmap using LockBits.
        private byte[] GetBytesFromBitmap(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int byteCount = bmpData.Stride * bmp.Height;
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(bmpData.Scan0, bytes, 0, byteCount);
            bmp.UnlockBits(bmpData);
            return bytes;
        }

        // Helper: Create a Bitmap from raw byte data.
        private Bitmap CreateBitmapFromBytes(byte[] bytes, int width, int height, PixelFormat format)
        {
            Bitmap bmp = new Bitmap(width, height, format);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, format);
            int byteCount = bmpData.Stride * height;
            Marshal.Copy(bytes, 0, bmpData.Scan0, byteCount);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private byte[] Compress(byte[] data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionLevel.Optimal))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        // Decompression method (for when you need to restore the diff)
        private int[] Decompress(byte[] compressedData)
        {
            using (MemoryStream input = new MemoryStream(compressedData))
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                {
                    gzip.CopyTo(output);
                }
                byte[] decompressedBytes = output.ToArray();

                // Convert byte[] back to int[]
                int[] result = new int[decompressedBytes.Length / sizeof(int)];
                Buffer.BlockCopy(decompressedBytes, 0, result, 0, decompressedBytes.Length);
                return result;
            }
        }

        public void SaveUndoState(Layer layer)
        {
            using (var ms = new MemoryStream())
            {
                layer.Bitmap.Save(ms, ImageFormat.Png);
                byte[] bitmapData = ms.ToArray();

                using (var zippedStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(zippedStream, CompressionLevel.Optimal))
                    {
                        gzipStream.Write(bitmapData, 0, bitmapData.Length);
                    }
                    byte[] zippedData = zippedStream.ToArray();
                    undoStack.Push(new UndoEntry(layer.LayerId, zippedData));
                }
            }
        }

        //public void ResizeBitmap(int layerid, int newWidth, int newHeight, byte side)
        //{
        //    Layer layer = GetLayer(layerid);
        //    Bitmap originalBitmap = layer.Bitmap;

        //    // Create a new Bitmap with the desired size
        //    if (newWidth <= 1 || newHeight <= 1)
        //    {
        //        layer.Bitmap = originalBitmap;
        //        return;
        //    }

//        Bitmap resizedBitmap;
//            switch (side)
//            {
//                case 0: // Top
//                        //layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
//                    if(originalBitmap.Height > newHeight)
//                    {
//                        resizedBitmap = new Bitmap(originalBitmap.Width, newHeight);
//    }
//                    else
//                    {
//                        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
//}

//break;
//                case 1:// Bottom
//    if (originalBitmap.Height > newHeight)
//    {
//        resizedBitmap = new Bitmap(originalBitmap.Width, newHeight);
//    }
//    else
//    {
//        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
//    }

//    break;
//case 2: // Right
//    if (originalBitmap.Width > newWidth)
//    {
//        resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
//    }
//    else
//    {
//        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
//    }
//    resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
//    break;
//case 3: // Left
//    if (originalBitmap.Width > newWidth)
//    {
//        resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
//    }
//    else
//    {
//        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
//    }
//    break;
//default:
//    return;
//    break;
//}

//    // Create a Graphics object to draw on the new Bitmap
//    using (Graphics graphics = Graphics.FromImage(resizedBitmap))
//    {
//        // Set high-quality settings for resizing

//        //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
//        //graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
//        //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

//        switch (side)
//        {
//            case 0: // Top
//                    //layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
//                graphics.DrawImage(originalBitmap, new Rectangle(0, newHeight - originalBitmap.Height, resizedBitmap.Width, resizedBitmap.Height));
//                //layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
//                //layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
//                break;
//            case 1:  // Bottom
//                graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
//                break;
//            case 2: // Right
//                graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
//                break;
//            case 3: // Left
//                graphics.DrawImage(originalBitmap, new Rectangle(newWidth - originalBitmap.Width, 0, resizedBitmap.Width, resizedBitmap.Height));
//                //layer.Offset = new Point(layer.Offset.X - (newWidth - originalBitmap.Width), layer.Offset.Y);
//                break;
//        }

//    }
//    layer.Bitmap = resizedBitmap;
//}

public void ResizeBitmap(int layerid, int newWidth, int newHeight, byte side)
        {
            Layer layer = GetLayer(layerid);
            Bitmap originalBitmap = layer.Bitmap;

            // Create a new Bitmap with the desired size
            if (newWidth <= 1 || newHeight <= 1)
            {
                layer.Bitmap = originalBitmap;
                return;
            }

            //Bitmap resizedBitmap = new Bitmap(newWidth, newHeight);

            Bitmap resizedBitmap;
            switch (side)
            {
                case 0: // Top
                        //layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
                    if (originalBitmap.Height > newHeight)
                    {
                        resizedBitmap = new Bitmap(originalBitmap.Width, newHeight);
                    }
                    else
                    {
                        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height + Math.Abs(originalBitmap.Height - newHeight));
                    }

                    break;
                case 1:// Bottom
                    if (originalBitmap.Height > newHeight)
                    {
                        resizedBitmap = new Bitmap(originalBitmap.Width, newHeight);
                    }
                    else
                    {
                        //resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
                        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height + Math.Abs(originalBitmap.Height - newHeight));
                    }

                    break;
                case 2: // Right
                    if (originalBitmap.Width > newWidth)
                    {
                        resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
                    }
                    else
                    {
                        resizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
                    }
                    resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
                    break;
                case 3: // Left
                    if (originalBitmap.Width > newWidth)
                    {
                        resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
                    }
                    else
                    {
                        resizedBitmap = new Bitmap(originalBitmap.Width + Math.Abs(originalBitmap.Width - newWidth), originalBitmap.Height);
                    }
                    break;
                default:
                    return;
                    break;
            }
            // Create a Graphics object to draw on the new Bitmap
            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {
                // Set high-quality settings for resizing

                //graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                //graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                switch (side)
                {
                    case 0: // Top
                        if (originalBitmap.Height > newHeight)
                        {
                            // make smaller
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
                            layer.Offset = new Point(layer.Offset.X, layer.Offset.Y - (newHeight - originalBitmap.Height));
                        }
                        else
                        {
                            // make bigger
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, originalBitmap.Height + Math.Abs(originalBitmap.Height - newHeight)));
                            layer.Offset = new Point(layer.Offset.X, layer.Offset.Y + (originalBitmap.Height - newHeight));
                        }
                 
                        break;
                    case 1: // Bottom
                        if (originalBitmap.Height > newHeight)
                        {
                            // make smaller
                            Console.WriteLine("originalBitmap.Height > newHeight");
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
                        }
                        else
                        {
                            // make bigger
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, originalBitmap.Height+ Math.Abs(originalBitmap.Height-newHeight)));
                        }
                     
                        break;
                    case 2: // Right
                            // make doesnt care
                        graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
                        break;
                    case 3: // Left
                        if (originalBitmap.Width > newWidth)
                        {
                            //resizedBitmap = new Bitmap(newWidth, originalBitmap.Height);
                            // make smaller
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, resizedBitmap.Width, resizedBitmap.Height));
                            layer.Offset = new Point(layer.Offset.X - (newWidth - originalBitmap.Width), layer.Offset.Y);
                        }
                        else
                        {
                            // make bigger
                            graphics.DrawImage(originalBitmap, new Rectangle(0, 0, originalBitmap.Width + Math.Abs(originalBitmap.Width - newWidth), resizedBitmap.Height));
                            layer.Offset = new Point(layer.Offset.X + (originalBitmap.Width - newWidth), layer.Offset.Y);
                        }
                        //layer.Offset = new Point(layer.Offset.X - (newWidth - originalBitmap.Width), layer.Offset.Y);
                        break;
                }
              
            }
            layer.Bitmap = resizedBitmap;
        }

        public static Bitmap RotateImage(Bitmap b, float angle)
        {
            // Original image dimensions
            int width = b.Width;
            int height = b.Height;

            Rectangle bounds = GetBoundingBox(b);
            Rectangle newbounds = GetBoundingBoxForRotation(b, angle);

            // Diagonal length of the image (bounding box)
            int diagonal = (int)Math.Ceiling(Math.Sqrt(width * width + height * height));

            // Create a new empty bitmap with a square size based on the diagonal
            Bitmap returnBitmap = new Bitmap(diagonal, diagonal);

            // Create a Graphics object to draw the rotated image
            Graphics g = Graphics.FromImage(returnBitmap);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.TranslateTransform((float)diagonal / 2, (float)diagonal / 2);
            //Rotate.        
            g.RotateTransform(angle);
            //Move image back.
            g.TranslateTransform(-(float)diagonal / 2, -(float)diagonal / 2);

            // Draw the original image into the new bitmap, which is now centered
            g.DrawImage(b, new Point((diagonal - width) / 2, (diagonal - height) / 2));

            returnBitmap = new Bitmap(CropImage(returnBitmap));
            //returnBitmap.Save("C:\\Users\\rlly\\Videos\\bimige.png", ImageFormat.Png);

            // crop image so it isnt full diagonal
            return CropImage(returnBitmap);
        }

        public static Bitmap CropImage(Bitmap originalImage)
        {
            // Get the bounding box of non-transparent pixels in the rotated image
            Rectangle boundingBox = GetBoundingBox(originalImage);

            // Create a new bitmap based on the bounding box size
            Bitmap croppedBitmap = new Bitmap(boundingBox.Width, boundingBox.Height);

            // Create a graphics object to draw the cropped part of the original image into the new bitmap
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                // Define the area to copy (from the bounding box)
                g.DrawImage(originalImage, new Rectangle(0, 0, boundingBox.Width, boundingBox.Height),
                  boundingBox, GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }
        //float diagonal = (float)Math.Sqrt(width * width + height * height);
        public static Rectangle GetBoundingBoxForRotation(Bitmap image, float angle)
        {
            // Get the original bounding box of non-transparent pixels
            Rectangle originalBoundingBox = GetBoundingBox(image);

            // Get the center of the original bounding box
            float centerX = originalBoundingBox.Left + originalBoundingBox.Width / 2.0f;
            float centerY = originalBoundingBox.Top + originalBoundingBox.Height / 2.0f;

            // Define the four corners of the original bounding box
            PointF[] corners = new PointF[] {
        new PointF(originalBoundingBox.Left, originalBoundingBox.Top), // Top-left corner
          new PointF(originalBoundingBox.Right, originalBoundingBox.Top), // Top-right corner
          new PointF(originalBoundingBox.Left, originalBoundingBox.Bottom), // Bottom-left corner
          new PointF(originalBoundingBox.Right, originalBoundingBox.Bottom) // Bottom-right corner
      };

            // Create a rotation matrix around the center of the image
            Matrix rotationMatrix = new Matrix();
            rotationMatrix.RotateAt(angle, new PointF(centerX, centerY));

            // Apply the rotation matrix to all four corners
            rotationMatrix.TransformPoints(corners);

            // Find the new bounding box for the rotated image
            float minX = corners.Min(c => c.X);
            float minY = corners.Min(c => c.Y);
            float maxX = corners.Max(c => c.X);
            float maxY = corners.Max(c => c.Y);

            // Return the rotated bounding box
            return new Rectangle(
              (int)Math.Floor(minX),
              (int)Math.Floor(minY),
              (int)Math.Ceiling(maxX - minX),
              (int)Math.Ceiling(maxY - minY)
            );
        }

        //// This method computes the bounding box of non-transparent pixels
        public static Rectangle GetBoundingBox(Bitmap image)
        {
            int minX = image.Width;
            int maxX = 0;
            int minY = image.Height;
            int maxY = 0;

            // Loop through each pixel of the image
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    // Get the pixel color at the current position
                    Color pixelColor = image.GetPixel(x, y);

                    // Check if the alpha component is greater than 0 (valid pixel)
                    if (pixelColor.A > 0)
                    {
                        // Update the min and max bounds for the bounding box
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // If no valid pixels are found, return an empty rectangle
            if (minX > maxX || minY > maxY)
                return Rectangle.Empty;

            // Return the bounding box as a Rectangle
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Undo the last action for a specific layer
        public void Undo(int layerId)
        {
            var tempStack = new Stack<UndoEntry>();
            while (undoStack.Count > 0)
            {
                var entry = undoStack.Pop();
                if (entry.LayerId == layerId)
                {
                    Bitmap restoredBitmap = UnzipBitmap(entry.ZippedBitmap);
                    foreach (var layer in layers)
                    {
                        if (layer.LayerId == layerId)
                        {
                            layer.Bitmap = restoredBitmap;
                            break;
                        }
                    }
                    break;
                }
                else
                {
                    tempStack.Push(entry);
                }
            }

            while (tempStack.Count > 0)
            {
                undoStack.Push(tempStack.Pop());
            }
        }

        public Layer GetLayer(int layerId)
        {
            return layers.Find(layer => layer.LayerId == layerId);
        }

        private Bitmap UnzipBitmap(byte[] zippedData)
        {
            using (var zippedStream = new MemoryStream(zippedData))
            using (var gzipStream = new GZipStream(zippedStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                outputStream.Position = 0;
                return new Bitmap(outputStream);
            }
        }

        public void ReorderLayers(int draggedLayerId, int targetLayerId)
        {
            var draggedLayer = layers.FirstOrDefault(layer => layer.LayerId == draggedLayerId);
            var targetLayer = layers.FirstOrDefault(layer => layer.LayerId == targetLayerId);

            if (draggedLayer != null && targetLayer != null)
            {
                int draggedIndex = layers.IndexOf(draggedLayer);
                int targetIndex = layers.IndexOf(targetLayer);

                // Swap the two layers
                layers[draggedIndex] = targetLayer;
                layers[targetIndex] = draggedLayer;

                UpdateLayerOrder(); // Ensure layers are ordered correctly
            }
        }

    }
}