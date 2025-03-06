using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Imaging;
using Label = System.Windows.Forms.Label;
using Color = System.Drawing.Color;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Control = System.Windows.Forms.Control;
using System.Drawing.Drawing2D;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.IO;


namespace Photoapp
{

    // as long as this is here this file gets treadted as a C# file not a desinger file
    //public class RemoveDesignerclass
    //{

    //}
    public partial class Form1 : Form
    {

   


        // function Modes
        public enum Mode
        {
            pencil, // done
            pen, // done
            rubber, // Done
            drag, // TODO
            eyedropper, // done
            zoom, // TODO FIX EVERYTHING
            font, // TODO
            rectangleSelect, // done
            freeSelect // done
        }

        //zooming panning
        private float zoomFactor = 1.0f;  // Default zoom factor
        private float zoomStep = 0.1f;    // Zoom step size
        private Point panOffset = new Point(0, 0);  // Offset for panning
        //private Point panStart = Point.Empty; // Starting point of the pan
        //private bool isPanning = false;    // Flag to indicate panning state


        private int selectedLayerId = 1; // Default to -1, indicating no layer is selected initially

        private bool isDrawing = false;
        private bool selectionactive = false;
        private Point lastPoint;
        private Mode currentMode = Mode.pencil; // Start in drawing mode


        private List<Point> points = new List<Point>(); // Store points for freehand drawing

        private Layer draggedLayer; // Track the dragged layer
        private int draggedLayerId; // Track the ID of the dragged layer
        private bool isDragging = false; // Track if a drag operation is ongoing
        private Panel draggedPanel = null; // Track the dragged panel (the layer being dragged)
        private Point mouseOffset; // Track the offset between the mouse pos

        // only redraw when needed
        //private Timer redrawTimer;
        //private bool needsRedraw = false;

        private Bitmap combinedBitmap;
        private Point selectionStartPoint;
        private Point selectionLastPoint;

        private Bitmap virtualCanvas; // for double buffering

        // previous bitmap of UI layer save then draw new one compare if SHIFT OR CTRL WAS HELD DOWN

        private Bitmap UILayer;



        // Bitmap before to make a difference
        byte[] dataOriginal;


        LayerManager layerManager = new LayerManager();

        MaskControl MaskControl = new MaskControl();

        // by my opinion correct masking

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
                    layer.Bitmap = loadedBitmap; // Assign the loaded image to the layer's bitmap
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }
        }


        public Form1()
        {

            InitializeComponent();
             this.KeyPreview = true;  // This allows the form to receive key events
            combinedBitmap = new Bitmap(canvasPanel.Width, canvasPanel.Height);
            MaskControl.MapRemembered = new byte[canvasPanel.Width, canvasPanel.Height];


            canvasPanel.MouseWheel += new MouseEventHandler(canvasPanel_MouseWheel);




            // border
            this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizin
            this.Text = "FoxToes";
            menuStrip1.Renderer = new CustomMenuRenderer();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            //canvasPanel.Paint += canvasPanel_Paint;
            // Offset the first menu item to the right
            ToolStripMenuItem firstItem = menuStrip1.Items[0] as ToolStripMenuItem;
            if (firstItem != null)
            {
                // Add padding to the left of the first item (this offsets it)
                firstItem.Padding = new Padding(20, 0, 0, 0); // 10px of padding on the left
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
            RedrawCanvas(new Rectangle(0, 0, canvasPanel.Width, canvasPanel.Height));
        }

        // layerpanel UI (refresh) direct form method
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
        private void clearUIBitmap()
        {
          
            if ((Control.ModifierKeys & Keys.Shift) == 0 && (Control.ModifierKeys & Keys.Control) == 0)
            {
              
                if (currentMode == Mode.rectangleSelect || currentMode == Mode.freeSelect)
                {
                    selectionactive = false;

                    // Dispose of UILayer and LastUILayer if they exist
                    if (UILayer != null)
                    {
                        UILayer.Dispose();
                        UILayer = null;
                    }
                    MaskControl.MapRemembered = new byte[canvasPanel.Width, canvasPanel.Height];
                    // Create new Bitmap instances with the size of canvasPanel.ClientRectangle
                    UILayer = new Bitmap(canvasPanel.ClientRectangle.Width, canvasPanel.ClientRectangle.Height);
                }
                else
                {
                    // Dispose of UILayer
                    if (UILayer != null)
                    {

                        UILayer.Dispose();
                        UILayer = null;
                    }
                    //Create a new UILayer
                    UILayer = new Bitmap(canvasPanel.ClientRectangle.Width, canvasPanel.ClientRectangle.Height);
                }

            }
            else
            {
                selectionactive = true;
                // Dispose of UILayer
                if (UILayer != null)
                {

                    UILayer.Dispose();
                    UILayer = null;
                }
                //Create a new UILayer
                UILayer = new Bitmap(canvasPanel.ClientRectangle.Width, canvasPanel.ClientRectangle.Height);
            }
        }

        private Point ClampPoint(Point p)
        {
            int x = Math.Max(0, Math.Min(canvasPanel.Width - 1, p.X));
            int y = Math.Max(0, Math.Min(canvasPanel.Height - 1, p.Y));
            return new Point(x, y);
        }

        // Canvas panel functions
        // Canvas panel functions
        private void RedrawCanvas(Rectangle invalidRect)
        {
            buildCombinedBitmap();
            canvasPanel.Invalidate(invalidRect);
        }
        static byte[] GetBytesFromBitmap(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int byteCount = bmpData.Stride * bmp.Height;
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(bmpData.Scan0, bytes, 0, byteCount);
            bmp.UnlockBits(bmpData);
            return bytes;

        }
        private void canvasPanel_MouseDown(object sender, MouseEventArgs e)
        {
   


            Point NormalizedPoint = NormalizeMousePosition(e.Location);
            NormalizedPoint = NormalizeMousePositionLayer(NormalizedPoint, layerManager.GetLayer(selectedLayerId).Offset);
            if (e.Button == MouseButtons.Left)
            {
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
                // save bitmap of Layer before changing
                dataOriginal = GetBytesFromBitmap(selectedLayer.Bitmap);
                isDrawing = true;
                lastPoint = NormalizedPoint;

                switch (currentMode)
                {
                    case Mode.pencil:
                        if (selectionactive)
                        {
                            NormalizedPoint = NormalizeMousePosition(e.Location);
                            lastPoint = NormalizedPoint;
                        }
                      
                        points.Clear();
                        points.Add(NormalizedPoint);
                     
                        break;

                    case Mode.rubber:
                        break;

                    case Mode.freeSelect:
                        NormalizedPoint = NormalizeMousePosition(e.Location);
                        clearUIBitmap();
                        points.Clear();
                        points.Add(NormalizedPoint);
                        break;

                    case Mode.rectangleSelect:
                        NormalizedPoint = NormalizeMousePosition(e.Location);
                        clearUIBitmap();
                        selectionStartPoint = NormalizedPoint;
                        break;
                    case Mode.pen:
                        points.Add(NormalizedPoint);
                        // If the last point is near the first point, close the shape
                        if (points.Count > 2 && IsNearFirstPoint(NormalizedPoint))
                        {
                            points.Remove(points[points.Count - 1]);
                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            {
                                //g.Clear(Color.White); // Clear the canvas first

                                // Draw lines between each pair of points
                                for (int i = 1; i < points.Count; i++)
                                {
                                    g.DrawLine(Pens.Black, points[i - 1], points[i]);
                                }

                                // Draw a line to close the shape (from the last point to the first point)
                                g.DrawLine(Pens.Black, points[points.Count - 1], points[0]);

                                // Optionally, draw a small dot for each point
                                foreach (var point in points)
                                {
                                    g.FillEllipse(Brushes.Black, point.X - 2, point.Y - 2, 4, 4); // Draw a small dot at each point
                                }
                            }

                            points.Clear(); // Optionally, clear the points list after closing the shape (or keep for next shape)
                            break;
                        }

                        // Only draw the lines between points (not a polygon)
                        if (points.Count > 1) // Only start drawing after the first point
                        {
                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            {
                                //g.Clear(Color.White); // Clear the canvas first

                                // Draw lines between each pair of points
                                for (int i = 1; i < points.Count; i++)
                                {
                                    g.DrawLine(Pens.Black, points[i - 1], points[i]);
                                }

                                // Optionally, draw a small dot for each point
                                foreach (var point in points)
                                {
                                    g.FillEllipse(Brushes.Black, point.X - 2, point.Y - 2, 4, 4); // Draw a small dot at each point
                                }
                            }
                        }

         
                        break;
                    case Mode.drag:
                        isDragging = true;
                        break;
                    case Mode.eyedropper:
                        // Get the color of the pixel at the given position
                        Color pixelColor = selectedLayer.Bitmap.GetPixel(e.X, e.Y);

                        // Extract the individual ARGB components
                        int alpha = pixelColor.A;
                        int red = pixelColor.R;
                        int green = pixelColor.G;
                        int blue = pixelColor.B;

                        var pointo=NormalizeMousePosition(NormalizedPoint);

                        // Update the legend text to display the individual components
                        legend.Text = $"A:{alpha};R:{red};G:{green};B:{blue};X:{e.X};Y:{e.Y};X:{pointo.X};Y:{pointo.Y}";
                        break;
                    case Mode.font:

                    break;
                }
            }
        }

        private bool IsNearFirstPoint(Point currentPoint)
        {
            // Check if the current point is near the first point (within 5px)
            var firstPoint = points[0];
            return (currentPoint.X - firstPoint.X) * (currentPoint.X - firstPoint.X) + (currentPoint.Y - firstPoint.Y) * (currentPoint.Y - firstPoint.Y) < 25;
        }


        private void canvasPanel_MouseMove(object sender, MouseEventArgs e)
        {

            Point NormalizedPoint = NormalizeMousePosition(e.Location);
            NormalizedPoint = NormalizeMousePositionLayer(NormalizedPoint, layerManager.GetLayer(selectedLayerId).Offset);
            if (isDrawing)
            {
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
                Rectangle invalidRect = Rectangle.Empty;
            
                switch (currentMode)
                {
                    case Mode.pencil:

                        if (selectionactive)  // If selection is active, draw on the UI layer
                        {
                            NormalizedPoint = NormalizeMousePosition(e.Location);
                            using (Graphics g = Graphics.FromImage(UILayer))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.DrawLine(Pens.Red, lastPoint, NormalizedPoint);
                            }
                            points.Add(NormalizedPoint);
                            invalidRect = GetBoundingRectangle(lastPoint, NormalizedPoint);
                            invalidRect = Rectangle.Inflate(invalidRect, 5, 5); // Inflate for pen size
                            lastPoint = NormalizedPoint;
                        }
                        else  // If no selection, draw directly on the selected bitmap layer
                        {
                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.DrawLine(Pens.Black, lastPoint, NormalizedPoint);
                            }
                            points.Add(NormalizedPoint);
                            invalidRect = canvasPanel.ClientRectangle; // Invalidate the entire canvas
                            //invalidRect = GetBoundingRectangle(lastPoint, NormalizedPoint); // redo
                            //invalidRect = Rectangle.Inflate(invalidRect, 5, 5); // Inflate for pen size
                            lastPoint = NormalizedPoint;
                        }
                        break;
                    case Mode.rubber:
                        if (selectionactive)
                        {
                            NormalizedPoint = NormalizeMousePosition(e.Location);
                            using (Graphics g = Graphics.FromImage(UILayer))
                            using (SolidBrush maskBrush = new SolidBrush(Color.FromArgb(128, Color.Gray))) // Semi-transparent preview
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.FillEllipse(maskBrush, NormalizedPoint.X - 10, NormalizedPoint.Y - 10, 20, 20);
                            }
                        }
                        else
                        {
                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            using (SolidBrush transparentBrush = new SolidBrush(Color.Transparent))
                            {
                                g.CompositingMode = CompositingMode.SourceCopy;
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.FillEllipse(transparentBrush, NormalizedPoint.X - 10, NormalizedPoint.Y - 10, 20, 20);
                            }
                            invalidRect = GetBoundingRectangle(lastPoint, NormalizedPoint);
                            invalidRect = Rectangle.Inflate(invalidRect, 20, 20); // Inflate for pen size
                            lastPoint = NormalizedPoint;
                        }
                        break;
                    case Mode.drag:
                        if (isDragging)
                        {
                            NormalizedPoint = NormalizeMousePosition(e.Location);
                            // Calculate the new location of the dragged panel
                            Point newLocation = new Point(NormalizedPoint.X, NormalizedPoint.Y);
                            selectedLayer.Offset = newLocation;
                        }
                        break;
                    case Mode.freeSelect:
                        NormalizedPoint = NormalizeMousePosition(e.Location);
                        Point clampedPoint = ClampPoint(NormalizedPoint);

                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            if (points.Count > 1)
                            {
                                g.DrawLine(Pens.Red, points[points.Count - 2], points.Last());
                            }

                            g.DrawLine(Pens.Red, points.Last(), clampedPoint);
                        }

                        points.Add(clampedPoint);
                        invalidRect = canvasPanel.ClientRectangle; // Invalidate the entire canvas
                        break;
                    // unstable
                    case Mode.rectangleSelect:
                        // Logic for rectangle selection drawing.
                        NormalizedPoint = NormalizeMousePosition(e.Location);
                        selectionLastPoint = ClampPoint(NormalizedPoint);

                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            int startX = Math.Min(selectionStartPoint.X, selectionLastPoint.X);
                            int startY = Math.Min(selectionStartPoint.Y, selectionLastPoint.Y);
                            int width = Math.Abs(selectionLastPoint.X - selectionStartPoint.X);
                            int height = Math.Abs(selectionLastPoint.Y - selectionStartPoint.Y);

                            g.DrawRectangle(Pens.Red, startX, startY, width, height);
                        }

                        invalidRect = canvasPanel.ClientRectangle; // Invalidate the entire canvas
                        break;
                }

                RedrawCanvas(invalidRect);
                //guide
                if (Mode.rectangleSelect == currentMode)
                {
                    clearUIBitmap();
                }
                    
            }
        }
 
        private void canvasPanel_MouseUp(object sender, MouseEventArgs e)
        {
            Point NormalizedPoint = NormalizeMousePosition(e.Location);
            NormalizedPoint = NormalizeMousePositionLayer(NormalizedPoint, layerManager.GetLayer(selectedLayerId).Offset);
            if (isDrawing)
            {
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
                Rectangle invalidRect = Rectangle.Empty;

                switch (currentMode)
                {
                    case Mode.pencil:
                        //using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                        //{
                        //    g.SmoothingMode = SmoothingMode.AntiAlias;
                        //    g.DrawLine(Pens.Black, lastPoint, NormalizedPoint);
                        //}
                        //points.Add(NormalizedPoint);
                        //invalidRect = GetBoundingRectangle(lastPoint, NormalizedPoint);
                        //invalidRect = Rectangle.Inflate(invalidRect, 10, 10);
                        //points.Clear();
                        if (selectionactive)
                        {
                            for (int x = 0; x < MaskControl.MapRemembered.GetLength(0); x++) // Loop through the width
                            {
                                for (int y = 0; y < MaskControl.MapRemembered.GetLength(1); y++) // Loop through the height
                                {
                                    // Check if the mask for this pixel is marked (e.g., 2 indicates rubber area)
                                    if (MaskControl.MapRemembered[x, y] == 2)
                                    {

                                        // Copy the pixel from selectedLayer to the UILayer
                                        Color selectedLayerColor = UILayer.GetPixel(x, y);

                                        // only apply to a existing bitmap
                                        if (selectedLayerColor.A != 0)  // Check for transparency (alpha = 0)
                                        {
                                            

                                            Point newcord = NormalizeMousePositionLayer(new Point(x, y),selectedLayer.Offset);

                                            //selectedLayer.Bitmap.SetPixel(x, y, Color.Black); // Apply to the UI layer
                                            if (newcord.X > 0 && newcord.Y > 0)
                                            {
                                                if(newcord.X < selectedLayer.Bitmap.Width && newcord.Y < selectedLayer.Bitmap.Height)
                                                {
                                                    selectedLayer.Bitmap.SetPixel(newcord.X, newcord.Y, Color.Black); // Apply to the UI layer
                                                }
                                               
                                            }
                                         
                                        }
                                    }
                                }
                            }
                        }
                        else // If no selection is active, draw directly on the selected bitmap layer
                        {
                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.DrawLine(Pens.Black, lastPoint, NormalizedPoint);  // Direct pencil drawing on the bitmap
                            }
                        }

                        // Add the new point to the drawing path
                        points.Add(NormalizedPoint);

                        // Calculate the bounding rectangle for the pencil stroke
                        invalidRect = GetBoundingRectangle(lastPoint, NormalizedPoint);
                        invalidRect = Rectangle.Inflate(invalidRect, 5, 5);  // Inflate for pen size

                        lastPoint = NormalizedPoint;  // Update last point

                        break;

                    //case Mode.rubber:
                    //    invalidRect = new Rectangle(e.X - 15, e.Y - 15, 30, 30);
                    //    break;
                    case Mode.rubber:
                        // Get the selected layer


                        // Loop through the MaskControl.MapRemembered and apply from the UILayer
                        // Loop through the MaskControl.MapRemembered and apply from selectedLayer to UILayer
                        if (selectionactive)
                        {
                            for (int x = 0; x < MaskControl.MapRemembered.GetLength(0); x++) // Loop through the width
                            {
                                for (int y = 0; y < MaskControl.MapRemembered.GetLength(1); y++) // Loop through the height
                                {
                                    // Check if the mask for this pixel is marked (e.g., 2 indicates rubber area)
                                    if (MaskControl.MapRemembered[x, y] == 2)
                                    {

                                        // Copy the pixel from selectedLayer to the UILayer
                                        Color selectedLayerColor = UILayer.GetPixel(x, y);

                                        if (selectedLayerColor.A != 0)  // Check for transparency (alpha = 0)
                                        {
                                            // Copy the pixel from selectedLayer to the UILayer
                                            Point newcord = NormalizeMousePositionLayer(new Point(x, y), selectedLayer.Offset);

                                            //selectedLayer.Bitmap.SetPixel(x, y, Color.Black); // Apply to the UI layer
                                            if (newcord.X > 0 && newcord.Y > 0)
                                            {
                                                if (newcord.X < selectedLayer.Bitmap.Width && newcord.Y < selectedLayer.Bitmap.Height)
                                                {
                                                    selectedLayer.Bitmap.SetPixel(newcord.X, newcord.Y, Color.Transparent); // Apply to the UI layer
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }
            
                        break;
                    case Mode.freeSelect:
                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.DrawLine(Pens.Red, points.Last(), points.First());
                        }
                        invalidRect = canvasPanel.ClientRectangle; // Invalidate the entire canvas
                        break;

                    case Mode.rectangleSelect:
                        NormalizedPoint = NormalizeMousePosition(e.Location);
                        selectionLastPoint = ClampPoint(NormalizedPoint);

                        //clearUIBitmap(); // fixes mask

                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            int startX = Math.Min(selectionStartPoint.X, selectionLastPoint.X);
                            int startY = Math.Min(selectionStartPoint.Y, selectionLastPoint.Y);
                            int width = Math.Abs(selectionLastPoint.X - selectionStartPoint.X);
                            int height = Math.Abs(selectionLastPoint.Y - selectionStartPoint.Y);

                            g.DrawRectangle(Pens.Red, startX, startY, width, height);
                        }
                        invalidRect = canvasPanel.ClientRectangle; // Invalidate the entire canvas
                        break;
                }


                if (currentMode == Mode.rectangleSelect || currentMode == Mode.freeSelect)
                {
                    if ((Control.ModifierKeys & Keys.Control) != 0)
                    {
                        MaskControl.MergeAndRemove(UILayer, Color.Blue);
                    }
                    else
                    {
                        MaskControl.MergeAndClearEdges(UILayer, Color.Blue);
                    }
                }
                else
                {
                    Savetomanager(selectedLayer.Bitmap);
                }
                clearUIBitmap();
                RedrawCanvas(invalidRect);
            }


                lastPoint = NormalizedPoint;
                isDrawing = false;
            // save selected layer bitmap to undo
         


        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            // Check if the Ctrl key, Shift key, and Z key are pressed
            if (e.Control && e.Shift && e.KeyCode == Keys.Z)
            {
                // Implement redo functionality here
                // Example: Call a redo method or logic

            }
            // Optionally, you can also handle Ctrl + Z for undo
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                //Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
                layerManager.RestoreFromUndoStack();
                RedrawCanvas(canvasPanel.ClientRectangle);

            }
        }
        private void Savetomanager(Bitmap Modified)
        {
            string diffOutputPath = @"C:\Users\rlly\Desktop\paint\current.png";
            Modified.Save(diffOutputPath, ImageFormat.Png);
            byte[] dataModified = GetBytesFromBitmap(Modified);
            // Create an array to hold the signed difference.
            // Since a difference can be negative, we use an int array.
            int[] signedDiff = new int[dataOriginal.Length];

            // Compute the signed difference for each channel.
            // (For each channel: diff = original - modified)
            for (int i = 0; i < dataOriginal.Length; i++)
            {
                signedDiff[i] = dataOriginal[i] - dataModified[i];
            }
            layerManager.SaveToUndoStack(selectedLayerId, signedDiff);
        }
     
        private void canvasPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control) // Zoom when Control is held
            {
                // Get mouse position relative to the canvas
                Point mousePos = e.Location;

                // Normalize mouse wheel movement
                float zoomStep = 0.1f;
                float zoomFactorChange = (e.Delta > 0) ? (1 + zoomStep) : (1 - zoomStep);

                // Calculate new zoom factor, clamp it between limits
                float newZoomFactor = zoomFactor * zoomFactorChange;
                newZoomFactor = Math.Max(0.1f, Math.Min(newZoomFactor, 5.0f)); // Clamp between 0.1x and 5x

                // Calculate scale difference
                float scaleChange = newZoomFactor / zoomFactor;

                // Adjust panOffset so that the point under the mouse stays in place
                panOffset.X = (int)(mousePos.X - scaleChange * (mousePos.X - panOffset.X));
                panOffset.Y = (int)(mousePos.Y - scaleChange * (mousePos.Y - panOffset.Y));

                // Update zoom factor
                zoomFactor = newZoomFactor;
              
                // Redraw the canvas with updated zoom and pan
                RedrawCanvas(canvasPanel.ClientRectangle);
            }
            else if (Control.ModifierKeys == Keys.Shift) // Horizontal pan when Shift is held
            {
                panOffset.X += e.Delta; // Adjust horizontal panning
                RedrawCanvas(canvasPanel.ClientRectangle);
            }
            else // Vertical pan when no modifier keys are held
            {
                panOffset.Y += e.Delta; // Adjust vertical panning
                RedrawCanvas(canvasPanel.ClientRectangle);
            }
        }



        private Rectangle GetBoundingRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);

            // Scale and translate based on zoom and pan
            int adjustedX = (int)(x * zoomFactor + panOffset.X);
            int adjustedY = (int)(y * zoomFactor + panOffset.Y);
            int adjustedWidth = (int)(width * zoomFactor);
            int adjustedHeight = (int)(height * zoomFactor);

            return new Rectangle(adjustedX, adjustedY, adjustedWidth, adjustedHeight);
        }
        private Point NormalizeMousePosition(Point mousePos)
        {
            // Adjust the mouse position based on the current zoom and pan
            float normalizedX = (mousePos.X - panOffset.X) / zoomFactor;
            float normalizedY = (mousePos.Y - panOffset.Y) / zoomFactor;
            return new Point((int)normalizedX, (int)normalizedY);
        }

        private Point NormalizeMousePositionLayer(Point mousePos,Point layer)
        {
            // Adjust the mouse position based on the current zoom and pan
            // Zoom is accounted for by the Normalize mouse position point
            float normalizedX = (mousePos.X - layer.X);
            float normalizedY = (mousePos.Y - layer.Y);
            return new Point((int)normalizedX, (int)normalizedY);
        }



        private void buildCombinedBitmap()
        {
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                //g.TranslateTransform(500, -500);
                // Apply scaling and panning
                g.TranslateTransform(panOffset.X, panOffset.Y);
                g.ScaleTransform(zoomFactor, zoomFactor);


                g.CompositingMode = CompositingMode.SourceOver;
                g.Clear(Color.Transparent);

                foreach (var layer in layerManager.GetLayers())
                {
                    if (layer.Bitmap != null)
                    {
                        g.DrawImage(layer.Bitmap, layer.Offset.X, layer.Offset.Y);
                    }

                }


                Bitmap maskBitmap = new Bitmap(
        MaskControl.MapRemembered.GetLength(0), // Width
        MaskControl.MapRemembered.GetLength(1)  // Height
    );

                using (Graphics maskGraphics = Graphics.FromImage(maskBitmap))
                {
                    maskGraphics.Clear(Color.Transparent); // Start with transparency

                    for (int x = 0; x < MaskControl.MapRemembered.GetLength(0); x++) // Width
                    {
                        for (int y = 0; y < MaskControl.MapRemembered.GetLength(1); y++) // Height
                        {
                            if (MaskControl.MapRemembered[x, y] == 1)
                            {
                             
                                // Set pixel to desired color (e.g., Red)
                                maskBitmap.SetPixel(x, y, Color.Red); 
                            }
                         //   testing purposes
                            //else if (MaskControl.MapRemembered[x, y] == 2)
                            //{
                            //    // Set pixel to desired color (e.g., Red)
                            //    maskBitmap.SetPixel(x, y, Color.Blue);
                            //}
                        }
                    }

                }

                // Draw maskBitmap onto combinedBitmap
                if (UILayer != null)
                {
                    g.DrawImage(UILayer, 0, 0);
                }
                g.DrawImage(maskBitmap, 0, 0);
                maskBitmap.Dispose(); // remove
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
            currentMode = Mode.rubber; // done
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
            label.Click += (sender, e) => {
                SelectLayer(layerId); // Update the selected layer label and variable
            };

            // Add the buttons to the layer item panel
            layerItemPanel.Controls.Add(upButton);
            layerItemPanel.Controls.Add(downButton);

            // Add the new layer panel to the LayerPanel
            layerPanel.Controls.Add(layerItemPanel);
        }


    }

}