using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;

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
        private Stack<UndoEntry> undoStack;
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

        private void SaveUndoState(Layer layer)
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