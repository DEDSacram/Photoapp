using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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

        public Layer(int layerId, Bitmap bitmap)
        {
            LayerId = layerId;
            Bitmap = bitmap;
            Order = 0; // Default order value
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

        public UndoEntry(int layerId, byte[] zippedBitmap)
        {
            LayerId = layerId;
            ZippedBitmap = zippedBitmap;
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

        public void SaveToUndoStack(int layerId, int[] diff)
        {
            // Convert int[] to byte[]
            byte[] diffBytes = new byte[diff.Length * 4];
            Buffer.BlockCopy(diff, 0, diffBytes, 0, diffBytes.Length);



            // Compress the byte array
            byte[] compressedData = Compress(diffBytes);

            // Save the compressed data in an UndoEntry
            UndoEntry entry = new UndoEntry(layerId, compressedData);
            undoStack.Push(entry);
        }
        public void RestoreFromUndoStack()
        {
            if (undoStack.Count == 0)
                return; // No undo available.

            // Get the last undo entry from the stack.

            UndoEntry entry = undoStack.Pop();

            // Retrieve the layer from the private list 'layers' by its ID.
            Layer layer = GetLayerById(entry.LayerId);
            if (layer == null)
                return; // Layer not found, nothing to restore.

            // Decompress the stored zipped difference data.
            // Decompress should return an int[] array with one difference per byte.
            int[] diff = Decompress(entry.ZippedBitmap);



            byte[] diffForDisplay = new byte[diff.Length];
            for (int i = 0; i < diff.Length; i++)
            {
                int shifted = diff[i] + 128;
                if (shifted < 0) shifted = 0;
                if (shifted > 255) shifted = 255;
                diffForDisplay[i] = (byte)shifted;
            }

            Bitmap diffBmp = CreateBitmapFromBytes(diffForDisplay, layer.Bitmap.Width, layer.Bitmap.Height, PixelFormat.Format32bppArgb);
            string diffOutputPath = @"C:\Users\rlly\Desktop\paint\signed_differenceeasdasdee.png";
            diffBmp.Save(diffOutputPath, ImageFormat.Png);



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
            Bitmap reconstructedBmp = CreateBitmapFromBytes(reconstructedData, layer.Bitmap.Width, layer.Bitmap.Height, PixelFormat.Format32bppArgb);
            string reconOutputPath = @"C:\Users\rlly\Desktop\paint\reconstructednew.png";
            reconstructedBmp.Save(reconOutputPath, ImageFormat.Png);
            reconstructedBmp.Dispose();


           



            //// Rebuild the bitmap from the modified byte array.
            //Bitmap restoredBitmap = CreateBitmapFromBytes(currentImage, layer.Bitmap.Width, layer.Bitmap.Height, PixelFormat.Format32bppArgb);

            //// Save the restored bitmap back into the layer.
            //layer.Bitmap = restoredBitmap;
            //string reconOutputPath = @"C:\Users\rlly\Desktop\paint\reconstructed222.png";
            //restoredBitmap.Save(reconOutputPath, ImageFormat.Png);
            ////string reconstructedPath = "C:\\Users\\rlly\\source\\repos\\ConsoleApp2\\ConsoleApp2\\reconstructed.png";

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