using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Forms.Integration;
using static Photoapp.Form1.CustomMenuRenderer;
using System.Windows.Controls;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.IO;
using System.Reflection.Emit;
using Label = System.Windows.Forms.Label;
using Color = System.Drawing.Color;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using static Photoapp.Form1;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using Control = System.Windows.Forms.Control;
using System.Reflection;
using System.Windows.Media.Media3D;
using System.Drawing.Drawing2D;
using FormsExtensions.Controls.Entrys;
using static System.Net.WebRequestMethods;

namespace Photoapp
{
  
    // UNIONIZING
    // REMOVING
    public partial class Form1: Form
    {

        private int selectedLayerId = 1; // Default to -1, indicating no layer is selected initially

        private bool isDrawing = false;
        private Point lastPoint;
        private Mode currentMode = Mode.pencil;  // Start in drawing mode
        private List<Point> points = new List<Point>();  // Store points for freehand drawing

        private Bitmap virtualCanvas; // for double buffering


        // previous bitmap of UI layer save then draw new one compare if SHIFT OR CTRL WAS HELD DOWN


        private Bitmap UILayer;
        private Bitmap LastUILayer;


        LayerManager layerManager = new LayerManager();



        // by my opinion correct masking
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



        // virtual canvas due to compositing when updating a layer in any way first draw onto this then take its bitmap save into layer and then trigger a repaint in the main UI one
        private void CreateVirtualCanvas()
        {
            // Create a new Bitmap based on the canvasPanel's size
            virtualCanvas = new Bitmap(canvasPanel.Width, canvasPanel.Height, PixelFormat.Format32bppArgb);

            // Get the selected layer's bitmap from the LayerManager
            var selectedLayerBitmap = layerManager.GetLayer(selectedLayerId).Bitmap;

            // Render the selected layer onto the virtual canvas
            using (Graphics g = Graphics.FromImage(virtualCanvas))
            {
                // Clear the canvas to be transparent
                g.Clear(Color.Transparent);

                // Draw the selected layer's bitmap onto the virtual canvas
                g.DrawImage(selectedLayerBitmap, 0, 0);
            }

            // At this point, the virtualCanvas contains the selected layer's bitmap
            // but no invalidation or rendering of the panel occurs.
        }
        // function Modes
        public enum Mode
        {
            pencil,  // Free drawing mode
            pen,
            rubber,  // Erase mode (optional for future extension)
            drag,
            eyedropper,
            zoom,
            font,
            rectangleSelect,
            freeSelect
        }
        // version and layer control
        public class Layer
        {
            public int LayerId { get; set; }
            public Bitmap Bitmap { get; set; }
            public int Order { get; set; }  // Added to track the order of layers

            public Layer(int layerId, Bitmap bitmap)
            {
                LayerId = layerId;
                Bitmap = bitmap;
                Order = 0; // Default order value
            }
        }
        public class UndoEntry
        {
            public int LayerId { get; private set; }
            public byte[] ZippedBitmap { get; private set; }

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
        // load image into layer bitmap
        private void LoadImageIntoLayer(string filePath, int layerId)
        {
            try
            {
                // Load the image from the file path
                Bitmap loadedBitmap = new Bitmap(filePath);

                // Set the loaded bitmap to the selected layer
                var layer = layerManager.GetLayer(layerId);
                if (layer != null)
                {
                    layer.Bitmap = loadedBitmap;  // Assign the loaded image to the layer's bitmap
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }
        }

     

        // dark menu strip ovveride
        public class CustomMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                // Clear the background before filling it
                e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(49, 54, 59)), e.Item.ContentRectangle);

                // Check if the item is selected or hovered
                if (e.Item.Selected)
                {
                    // Optional: slightly change the background when selected
                    e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(79, 84, 89)), e.Item.ContentRectangle);
                    e.Item.ForeColor = System.Drawing.Color.White;  // Text color for hovered item
                }
                else
                {
                    e.Item.ForeColor = System.Drawing.Color.White;  // Text color for non-hovered items
                }
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                // Clear the background before filling it
                e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(49, 54, 59)), e.AffectedBounds);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                // Draw the separator color correctly
                e.Graphics.FillRectangle(System.Drawing.Brushes.Gray, e.Item.ContentRectangle);
            }
        }
        // enable double buffer




        private Layer draggedLayer;  // Track the dragged layer
        private int draggedLayerId;  // Track the ID of the dragged layer
        private bool isDragging = false;  // Track if a drag operation is ongoing
        private Panel draggedPanel = null;  // Track the dragged panel (the layer being dragged)
        private Point mouseOffset;  // Track the offset between the mouse pos


        // only redraw when needed
        //private Timer redrawTimer;
        //private bool needsRedraw = false;


        private Bitmap combinedBitmap;
        private Point selectionStartPoint;
        private Point selectionLastPoint;

        public Form1()
        {
     
            InitializeComponent();
           
            combinedBitmap = new Bitmap(canvasPanel.Width, canvasPanel.Height);

            // border
            this.FormBorderStyle = FormBorderStyle.Sizable;  // Allow resizin
            this.Text = "FoxToes";
            menuStrip1.Renderer = new CustomMenuRenderer();
          


            //canvasPanel.Paint += canvasPanel_Paint;
            // Offset the first menu item to the right
            ToolStripMenuItem firstItem = menuStrip1.Items[0] as ToolStripMenuItem;
            if (firstItem != null)
            {
                // Add padding to the left of the first item (this offsets it)
                firstItem.Padding = new Padding(20, 0, 0, 0);  // 10px of padding on the left
            }
        }


      

        // Initialize
        private void Form1_Load(object sender, EventArgs e)
        {
            // Create a bitmap with the size of the canvasPanel
            Bitmap bitmap = new Bitmap(canvasPanel.Width, canvasPanel.Height);

            // Draw the canvasPanel onto the bitmap
            canvasPanel.DrawToBitmap(bitmap, new Rectangle(0, 0, canvasPanel.Width, canvasPanel.Height));

            // Generate a unique layer ID using GetNextLayerId
            int layerId = layerManager.GetNextLayerId();

            // Add the bitmap to the LayerManager as a new layer
            layerManager.AddLayer(layerId, bitmap);
            layerManager.AddLayer(layerId + 1, bitmap);

        
            // Update the LayerPanel to display the new layer
            AddLayerToLayerPanel(layerId);
            AddLayerToLayerPanel(layerId + 1);
            LoadImageIntoLayer(@"C:\Users\rlly\Pictures\fox.png", layerId + 1);
            CreateVirtualCanvas();
            CreateTransparentLayer();
            
        
        }


   

        // layerpanel UI (refresh)
        private void UpdateUIOrder()
        {
            // Clear existing controls from the layerPanel
            layerPanel.Controls.Clear();

            // Add panels back to the layerPanel in the updated order
            foreach (var layer in layerManager.GetLayers())
            {
                AddLayerToLayerPanel(layer.LayerId);
            }

            // Optionally, you could also add logic to scroll the panel back to the top after update
            if (layerPanel.Controls.Count > 0)
            {
                layerPanel.ScrollControlIntoView(layerPanel.Controls[0]);
            }

            // Trigger the canvasPanel repaint to reflect the updated layer order
            buildCombinedBitmap();
            canvasPanel.Invalidate();
        }

        // for drawing ui gets on top
        private void CreateTransparentLayer()
        {
            // Create a new bitmap with the dimensions of the canvasPanel and a pixel format that supports transparency.
            UILayer = new Bitmap(canvasPanel.Width, canvasPanel.Height, PixelFormat.Format32bppArgb);
            // Clear the bitmap to make it fully transparent.
            using (Graphics g = Graphics.FromImage(UILayer))
            {
                g.Clear(Color.Transparent);
            }
        }


        // layermanager moveup
        private void MoveLayerUp(int layerId)
        {
            var layer = layerManager.GetLayers().FirstOrDefault(l => l.LayerId == layerId);
            if (layer != null)
            {
                int currentIndex = layerManager.GetLayers().IndexOf(layer);
                if (currentIndex > 0) // Ensure it's not already at the top
                {
                    var aboveLayer = layerManager.GetLayers()[currentIndex - 1];
                    layerManager.ReorderLayers(layer.LayerId, aboveLayer.LayerId); // Reorder layers
                    UpdateUIOrder(); // Update the UI after reordering
                }
            }
        }
        // layermanager moveup
        private void MoveLayerDown(int layerId)
        {
            var layer = layerManager.GetLayers().FirstOrDefault(l => l.LayerId == layerId);
            if (layer != null)
            {
                int currentIndex = layerManager.GetLayers().IndexOf(layer);
                if (currentIndex < layerManager.GetLayers().Count - 1) // Ensure it's not already at the bottom
                {
                    var belowLayer = layerManager.GetLayers()[currentIndex + 1];
                    layerManager.ReorderLayers(layer.LayerId, belowLayer.LayerId); // Reorder layers
                    UpdateUIOrder(); // Update the UI after reordering
                }
            }
        }
        // layerpanel UI (Draw)
        private void AddLayerToLayerPanel(int layerId)
        {
            // Create a new label to represent the layer visually
            Label label = new Label
            {
                Text = $"Layer {layerId}",
                Width = layerPanel.Width - 60, // Fit within the width of the parent Panel minus space for buttons
                Height = 50, // Give some height for the label
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(49, 54, 59),
                Location = new Point(0, 0) // Place label at the top-left corner of the panel
            };

            // Create a panel to hold the label and buttons
            Panel layerItemPanel = new Panel
            {
                Width = layerPanel.Width, // Fit within the width of the parent Panel
                Height = 50, // Height includes some padding for spacing
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0, 0, 0, 0) // Remove any padding that could interfere
            };

            // Add the label to the layer item panel
            layerItemPanel.Controls.Add(label);

            // Create the "Up" button
            Button upButton = new Button
            {
                Text = "Down",
                Width = 50,
                ForeColor = Color.White,
                Height = layerItemPanel.Height / 2,
                Location = new Point(layerPanel.Width - 60, 0) // Position at the top of the panel
            };
            upButton.Click += (sender, e) => MoveLayerUp(layerId);

            // Create the "Down" button
            Button downButton = new Button
            {
                Text = "Up",
                Width = 50,
                ForeColor = Color.White,
                Height = layerItemPanel.Height / 2,
                Location = new Point(layerPanel.Width - 60, layerItemPanel.Height / 2) // Position at the bottom of the panel
            };
            downButton.Click += (sender, e) => MoveLayerDown(layerId);

            // Add a click event to select this layer when clicked
            label.Click += (sender, e) =>
            {
                SelectLayer(layerId); // Update the selected layer label and variable
            };

            // Add the buttons to the layer item panel
            layerItemPanel.Controls.Add(upButton);
            layerItemPanel.Controls.Add(downButton);

            // Add the new layer panel to the LayerPanel
            layerPanel.Controls.Add(layerItemPanel);
        }
        // layerpanel UI SELECT CURRENT
        private void SelectLayer(int layerId)
        {
            // selected layer label
            selectedLayerId = layerId; // Store the selected layer ID
            selectedLayer.Text = $"Selected Layer: {layerId}"; // Update the label to show the selected layer

            // selected layer
            // Load the current selected layer's bitmap into the virtual canvas
            Layer selected = layerManager.GetLayer(selectedLayerId);
            virtualCanvas = new Bitmap(selected.Bitmap);
        }

        public static void SaveBitmap(Bitmap bitmap, string filePath, ImageFormat format)
        {
            try
            {
                bitmap.Save(filePath, format);
                Console.WriteLine($"Bitmap saved successfully to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bitmap: {ex.Message}");
            }
        }
        public int invert(int max, int value, int min)
        {
            return (max - value + min);
        }

        public Bitmap CalcreturnFull(Bitmap newBitmap)
        {
            if (newBitmap == null)
            {
                return null;
            }
            // make it +1 bigger but do not count it into it on each side meaning +2

            int width = newBitmap.Width +2;
            int height = newBitmap.Height+2;

            int[,] imageColors = new int[width, height];
            //imageColors[0, 0] = 0;
            //imageColors[0, height-1] = 0;
            //imageColors[width-1, 0] = 0;
            //imageColors[height - 1, width - 1] = 0;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    Color pixelColor = newBitmap.GetPixel(x-1, y-1);
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
                    if(result[x, y] == 0)
                    {
                        newBitmap.SetPixel(x-1, y-1, Color.Transparent);
                    }else if (result[x,y] == 1) // for now also blue
                    {
                        newBitmap.SetPixel(x-1, y-1, Color.Blue);
                    }
                    else
                    {
                        newBitmap.SetPixel(x - 1, y - 1, Color.Blue);
                    }
                       
                }
            }


            // Now 'imageColors' contains the color data of the bitmap.
            // You can perform your calculations here.

            // Example: Print some color values (for debugging)
            /*
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Console.WriteLine($"Pixel ({x}, {y}): {imageColors[x, y]}");
                }
            }
            */

            // ... your image processing logic using imageColors ...

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
            SaveBitmap(result, "numberone.png", ImageFormat.Png);


            return result;
        }






        private void clearUIBitmap()
        {
            LastUILayer = new Bitmap(UILayer);

            if ((Control.ModifierKeys & Keys.Shift) == 0 && (Control.ModifierKeys & Keys.Control) == 0)
            {
                using (Graphics g = Graphics.FromImage(UILayer))
                {
                    g.Clear(Color.Transparent);
                }
                using (Graphics g = Graphics.FromImage(LastUILayer))
                {
                    g.Clear(Color.Transparent);
                }
         
            }
            else
            {
                // Clear the UILayer bitmap to remove any previous selection feedback.
                using (Graphics g = Graphics.FromImage(UILayer))
                {
                    g.Clear(Color.Transparent);
                }
            }
        }

     


        private void ExportCanvas(string filePath, Bitmap inputBitmap)
        {
            // Create a bitmap with the same size as the canvasPanel
            Bitmap bitmap = new Bitmap(canvasPanel.Width, canvasPanel.Height);

            // Use a Graphics object to draw on the bitmap
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Draw the input bitmap onto the new bitmap (if provided)
                if (inputBitmap != null)
                {
                    g.DrawImage(inputBitmap, new Rectangle(0, 0, canvasPanel.Width, canvasPanel.Height));
                }

                // Draw the canvasPanel content onto the bitmap
                canvasPanel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }

            // Save the bitmap to the specified file path
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

            // Release resources
            bitmap.Dispose();
        }

        // Canvas panel functions

        private void canvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = true;
                lastPoint = e.Location;


           


                switch (currentMode)
                {
                    case Mode.pencil:
                        points.Clear();
                        points.Add(lastPoint);
                        break;

                    case Mode.rubber:
                        // Prepare for erasing
                        break;
                    case Mode.freeSelect:
                        clearUIBitmap();
                        points.Clear();
                        points.Add(lastPoint);  // Start collecting points for the freehand selection
                        break;

                    case Mode.rectangleSelect:
                        clearUIBitmap();
                        selectionStartPoint = e.Location;  // Starting point for the rectangle
                        break;
                }
            }
        }
        // regional edits would be great to avoid checking whole bitmap
        private void canvasPanel_MouseMove(object sender, MouseEventArgs e)
        {
         
            if (isDrawing)
            {
                //// Check if the mouse is within canvasPanel bounds solves two issues
                //if (e.X < 0 || e.Y < 0 || e.X >= canvasPanel.Width || e.Y >= canvasPanel.Height)
                //{
                //    return; // Exit if out of bounds
                //}
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);

                switch (currentMode)
                {
                    case Mode.pencil:
                        using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.DrawLine(Pens.Black, lastPoint, e.Location); // Draw directly on the layer's bitmap
                        }
                        points.Add(e.Location);
                        lastPoint = e.Location;
                        break;

                    case Mode.rubber:
                        using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy; // Ensure transparency is drawn
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillEllipse(new SolidBrush(Color.Transparent), e.X - 10, e.Y - 10, 20, 20); // Erase area
                        }
                        break;
                    case Mode.freeSelect:
                        // Manually clamp mouse position to canvasPanel bounds
                        int clampedX = e.X < 0 ? 0 : (e.X >= canvasPanel.Width ? canvasPanel.Width - 1 : e.X);
                        int clampedY = e.Y < 0 ? 0 : (e.Y >= canvasPanel.Height ? canvasPanel.Height - 1 : e.Y);
                        Point clampedPoint = new Point(clampedX, clampedY);

                        // Draw freehand selection on the UILayer
                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            if (points.Count > 1)
                            {
                                g.DrawLines(Pens.Red, points.ToArray()); // Draw the freehand path
                            }
                            g.DrawLine(Pens.Red, points.Last(), clampedPoint); // Draw to clamped position
                        }

                        points.Add(clampedPoint); // Add the clamped point to the path
                        break;

                    case Mode.rectangleSelect:
                        // do nth
                        break;
                }

                // Redraw the combined bitmap and refresh the canvas
                buildCombinedBitmap();
                canvasPanel.Invalidate();
            }
        }

        private void canvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);

                switch (currentMode)
                {
                    case Mode.pencil:
                        using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.DrawLine(Pens.Black, lastPoint, e.Location);
                        }
                        points.Add(e.Location);
                        break;

                    case Mode.rubber:
                        // Erasing is already applied directly to the layer's bitmap
                        break;
                    case Mode.freeSelect:
                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            // Draw the final line connecting the last point to the first point
                            g.DrawLine(Pens.Red, points.Last(), points.First());

             
                        }
                   
                        break;
                    case Mode.rectangleSelect:
                        // Manually clamp mouse position to canvasPanel bounds
                        int clampedX = e.X < 0 ? 0 : (e.X >= canvasPanel.Width ? canvasPanel.Width - 1 : e.X);
                        int clampedY = e.Y < 0 ? 0 : (e.Y >= canvasPanel.Height ? canvasPanel.Height - 1 : e.Y);
                        selectionLastPoint = new Point(clampedX, clampedY);

                        // Draw rectangle selection on the UILayer
                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            // Calculate top-left corner and positive width/height
                            int startX = Math.Min(selectionStartPoint.X, selectionLastPoint.X);
                            int startY = Math.Min(selectionStartPoint.Y, selectionLastPoint.Y);
                            int width = Math.Abs(selectionLastPoint.X - selectionStartPoint.X);
                            int height = Math.Abs(selectionLastPoint.Y - selectionStartPoint.Y);

                            g.DrawRectangle(Pens.Red, startX, startY, width, height); // Draw the rectangle
                        }
                        break;

                }
                if (LastUILayer != null)
                {
                
                  
                        UILayer = new Bitmap(MergeAndClearEdges(UILayer, LastUILayer, Color.Blue));

                 

                }
                else
                {
                    LastUILayer = new Bitmap(UILayer);

                }


                    lastPoint = e.Location;
                isDrawing = false;

                // Update the combined bitmap and refresh the canvas
                buildCombinedBitmap();
                canvasPanel.Invalidate();
            }
        }


        private void buildCombinedBitmap()
        {
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                // Ensure transparency blending
                g.CompositingMode = CompositingMode.SourceOver;
                g.Clear(Color.Transparent);  // Clear with transparency to support layer blending

                // Draw all layers except the UI layer
                foreach (var layer in layerManager.GetLayers())
                {
                    if (layer.Bitmap != null)
                    {
                        g.DrawImage(layer.Bitmap, 0, 0);  // Draw each layer at (0, 0)
                    }
                }

                // Now draw the UI layer on top
                if (UILayer != null)
                {
                    g.DrawImage(UILayer, 0, 0);  // Draw the UI layer on top
                }
            }
        }


        // before paint first calculate the combined bitmap you are doing the paint to the buffer here for some reason it is even slower now
        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {     

            // Draw the combined bitmap instead of individual layers
            if (combinedBitmap != null)
            {
                e.Graphics.DrawImage(combinedBitmap, 0, 0);
            }
        }
        // UI buttons handle
        private void pencilButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.pencil;
        }
        private void penButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.pen;
        }
        private void dragButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.drag;
        }
        private void rubberButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.rubber;
        }
        private void eyeButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.eyedropper;
        }
        private void zoomButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.zoom;
        }
        private void fontButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.font;
        }
        private void selectBoxButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.rectangleSelect;
            //layerManager.PrintLayerOrder();
        }
        private void selectFreeButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.freeSelect;
            // Show a SaveFileDialog to select where to save the image
            //SaveFileDialog saveFileDialog = new SaveFileDialog
            //{
            //    Filter = "PNG Image|*.png|JPEG Image|*.jpg",
            //    Title = "Save Canvas as Image"
            //};

            //if (saveFileDialog.ShowDialog() == DialogResult.OK)
            //{
            //    ExportCanvas(saveFileDialog.FileName);
            //}
        }
    }


}
