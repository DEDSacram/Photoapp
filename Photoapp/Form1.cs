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
        float zoomStep = 0.1f;
        private Point panOffset = new Point(0, 0);  // Offset for panning


        private int selectedLayerId = 1; // Default to -1, indicating no layer is selected initially

        // check if mouse is pressed down otherwise do nothing
        private bool isDrawing = false;
        // check if is mask active
        private bool selectionactive = false;
        // 
        private Point lastPoint;
        // current mode that is being used
        private Mode currentMode = Mode.font; // Start in drawing mode


        private List<Point> points = new List<Point>(); // Store points for freehand drawing pen etc.

        private bool isDragging = false; // Track if a drag operation is ongoing
        private bool isRotating = false; // Track if a rotate operation is ongoing
        private bool isScaling = false; // Track if a scale operation is ongoing

        private Point previous = Point.Empty; // Track the previous mouse position for panning
        private Point previousOffset = Point.Empty;


        private Point startingpoint = Point.Empty; // Just normalized

        private Bitmap combinedBitmap;
        private Point selectionStartPoint;
        private Point selectionLastPoint;


        // previous bitmap of UI layer save then draw new one compare if SHIFT OR CTRL WAS HELD DOWN

        private Bitmap UILayer;



        // Bitmap before to make a difference
        byte[] dataOriginal;


        LayerManager layerManager = new LayerManager();

        MaskControl MaskControl = new MaskControl();


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

        private string GetPathFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            openFileDialog.Title = "Select an image file";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return string.Empty;
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
            AddLayerToLayerPanel(layerId); // Add the new layer to the LayerPanel

            // Update the LayerPanel to display the new layer
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
                if (currentMode == Mode.rectangleSelect || currentMode == Mode.freeSelect)
                {
                    selectionactive = true;
                }
              
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
            startingpoint = NormalizedPoint;
       
            NormalizedPoint = NormalizeMousePositionLayer(NormalizedPoint, layerManager.GetLayer(selectedLayerId).Offset);
            if (e.Button == MouseButtons.Left)
            {
                Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
                previous = new Point(selectedLayer.Bitmap.Width, selectedLayer.Bitmap.Height);
                previousOffset = selectedLayer.Offset;
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
                    case Mode.eyedropper:
                        // Get the color of the pixel at the given position
                        try
                        {

                      
                        Color pixelColor = selectedLayer.Bitmap.GetPixel(NormalizedPoint.X, NormalizedPoint.Y);

                        // Extract the individual ARGB components
                        int alpha = pixelColor.A;
                        int red = pixelColor.R;
                        int green = pixelColor.G;
                        int blue = pixelColor.B;

                        // Update the legend text to display the individual components
                        legend.Text = $"A:{alpha};R:{red};G:{green};B:{blue};X:{e.X};Y:{e.Y};";
                        }
                        catch
                        {

                        }
                        break;
                    case Mode.font:

                    break;
                    case Mode.drag:

                        break;
                }
            }
        }

       private void ResetModes() {
            isDragging = false;
            isRotating = false;
            isScaling = false;
        }
        private bool IsNearFirstPoint(Point currentPoint)
        {
            // Check if the current point is near the first point (within 5px)
            var firstPoint = points[0];
            return (currentPoint.X - firstPoint.X) * (currentPoint.X - firstPoint.X) + (currentPoint.Y - firstPoint.Y) * (currentPoint.Y - firstPoint.Y) < 25;
        }

        float lastAngle = -1; // Store the last applied rotation angle
        static double GetAngleBetweenPoints(Point lastPoint, Point normalizedPoint)
        {
            // Calculate differences in X and Y
            double dx = lastPoint.X - normalizedPoint.X;
            double dy = lastPoint.Y - normalizedPoint.Y;

            // Calculate the angle in radians
            double angleRadians = Math.Atan2(dy, dx);

            // Convert the angle to degrees
            double angleDegrees = angleRadians * (180.0 / Math.PI);

            return angleDegrees;
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
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                                //g.SmoothingMode = SmoothingMode.AntiAlias;
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
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                                //g.SmoothingMode = SmoothingMode.AntiAlias;
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
                      
                            if (isRotating)
                            {
                                int middleX = selectedLayer.Bitmap.Width / 2;
                                int middleY = selectedLayer.Bitmap.Height / 2;
                                
                                double startangle = GetAngleBetweenPoints(lastPoint, new Point(middleX, middleY));
                                double adjustedandlge = GetAngleBetweenPoints(NormalizedPoint, new Point(middleX, middleY));
                                double moveby = adjustedandlge - startangle;
                      
                                if (Math.Abs(moveby - lastAngle) > 0.1f) // A small tolerance to account for floating point precision
                                {
                                    selectedLayer.Bitmap = LayerManager.RotateImage(selectedLayer.Bitmap, (float)moveby);
                                    lastAngle = (float)moveby; // Update the last applied angle
                                    lastPoint = NormalizedPoint;
                                }
                                break;
                            }
                            if (isScaling)
                            {
                            NormalizedPoint = NormalizeMousePosition(e.Location);
            

                            // DI SE ZABIT ONREJI PROSIM NEPROHAZUJ SIRKU VYSKU OSY
                                           //   BEZ KONTEXTU                            // normalizovana
                            int centerx = selectedLayer.Bitmap.Width / 2 + selectedLayer.Offset.X;
                            int centery = selectedLayer.Bitmap.Height / 2 + selectedLayer.Offset.Y;

                 

                            int x = centerx - NormalizedPoint.X; // Example x-coordinate
                            int y = centery - NormalizedPoint.Y; // Example y-coordinate 

                            using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                            {
                                using (SolidBrush maskBrush = new SolidBrush(Color.FromArgb(128, Color.Red))) // Semi-transparent preview
                                {
                                    g.SmoothingMode = SmoothingMode.AntiAlias;
                                    g.FillEllipse(maskBrush, selectedLayer.Bitmap.Width / 2 -1, selectedLayer.Bitmap.Height / 2 - 1, 2, 2);
                                }
                            }



                            byte side = 0;
                            int rescalexby = startingpoint.X - NormalizedPoint.X;
                            int rescaleyby = startingpoint.Y - NormalizedPoint.Y;
                            // Declare a variable to store the closest side
                            string closestSide;
                            if (Math.Abs(x) > Math.Abs(y))
                            {
                                if (x > 0)
                                {
                                    closestSide = "Left";
                                    side = 3;
                                    rescalexby = -rescalexby;
                                }
                                else
                                {
                                    closestSide = "Right";
                                  
                                    side = 2;
                                }
                            }
                            else
                            {
                                if (y > 0)
                                {
                                    closestSide = "Top";
                                    side = 0;
                                    rescaleyby = -rescaleyby;
                                }
                                else
                                {
                                    closestSide = "Bottom";
                                
                                    side = 1;
                                }
                            }
                            //Console.WriteLine("Make smaller by: {0}", rescalexby);


                            //ResizeBitmap(Bitmap originalBitmap, int newWidth, int newHeight)
                            int newWidth = selectedLayer.Bitmap.Width - rescalexby;
                            int newHeight = selectedLayer.Bitmap.Height - rescaleyby;
                            
                       
                        
                            startingpoint = NormalizedPoint;
                            //Console.WriteLine("Closest side: {0}", closestSide);
                            layerManager.ResizeBitmap(selectedLayerId, newWidth, newHeight, side);

                            break;
                            }
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
                    case Mode.drag:
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


                    if(currentMode == Mode.drag && (isRotating || isScaling))
                    {
                        // Save the modified layer bitmap to the undo stack
                 
                        Savetomanager(selectedLayer.Bitmap,true);
                    }
                    else
                    {
                        // Save the modified layer bitmap to the undo stack
                        Savetomanager(selectedLayer.Bitmap,false);
                    }
                    // Savetomanager(selectedLayer.Bitmap); currently stop for rotate transforms
                }
                clearUIBitmap();
                RedrawCanvas(invalidRect);
            }


                lastPoint = NormalizedPoint;
                isDrawing = false;
            // save selected layer bitmap to undo
         


        }
        private void Savetomanager(Bitmap Modified, bool ovveride)
        {
            //string diffOutputPath = @"C:\Users\rlly\Desktop\paint\current.png";
            //Modified.Save(diffOutputPath, ImageFormat.Png);

            int[] signedDiff = new int[dataOriginal.Length];
            if (ovveride)
            {
                for (int i = 0; i < dataOriginal.Length; i++)
                {
                    signedDiff[i] = dataOriginal[i];
                }
                layerManager.SaveToUndoStack(selectedLayerId, signedDiff, ovveride, previous,previousOffset);
                return;
            }

            byte[] dataModified = GetBytesFromBitmap(Modified);

            // Create an array to hold the signed difference.
            // Since a difference can be negative, we use an int array.
            //int[] signedDiff = new int[dataOriginal.Length];

            // Compute the signed difference for each channel.
            // (For each channel: diff = original - modified)
            for (int i = 0; i < dataOriginal.Length; i++)
            {
                signedDiff[i] = dataOriginal[i] - dataModified[i];
            }
            layerManager.SaveToUndoStack(selectedLayerId, signedDiff, ovveride, previous,previousOffset);
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

            if (currentMode == Mode.drag)
            {
                if (e.KeyCode == Keys.T)
                {
                    isDragging = true;
                    isRotating = false;
                    isScaling = false;
                }
                // Rot
                if (e.KeyCode == Keys.R)
                {
                    isRotating = true;
                    isDragging = false;
                    isScaling = false;

                }
                // ScaleTransform
                if (e.KeyCode == Keys.S)
                {
                    isScaling = true;
                    isRotating = false;
                    isDragging = false;
                }

            }
        }
    
     
        private void canvasPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control) // Zoom when Control is held
            {
                // Get mouse position relative to the canvas
                Point mousePos = e.Location;

                // Normalize mouse wheel movement
                float zoomFactorChange = (e.Delta > 0) ? (1 + zoomStep) : (1 - zoomStep);

                // Calculate new zoom factor, clamp it between limits
                float newZoomFactor = zoomFactor * zoomFactorChange;
                newZoomFactor = Math.Max(0.1f, Math.Min(newZoomFactor, 100.0f)); // Clamp between 0.1x and 5x

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
                //g.RotateTransform(20);

                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
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
        private void SaveImage(ImageFormat format)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Image files (*.png, *.jpg, *.bmp) | *.png; *.jpg; *.bmp";
                saveFileDialog.Title = "Save an Image File";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Assuming combinedBitmap is the image you want to save
                        combinedBitmap.Save(saveFileDialog.FileName, format);
                        MessageBox.Show("Image saved successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving image: {ex.Message}");
                    }
                }
            }
        }
        // https://stackoverflow.com/questions/687261/converting-rgb-to-grayscale-intensity 5
        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap grayscaleBitmap = new Bitmap(original.Width, original.Height);
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    int grayScale = (int)((originalColor.R * 0.3) + (originalColor.G * 0.59) + (originalColor.B * 0.11));
                    Color grayColor = Color.FromArgb(originalColor.A, grayScale, grayScale, grayScale);
                    grayscaleBitmap.SetPixel(x, y, grayColor);
                }
            }
            return grayscaleBitmap;
        }

        private Bitmap InverseBitmap(Bitmap original)
        {
            Bitmap invertedBitmap = new Bitmap(original.Width, original.Height);
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    Color invertedColor = Color.FromArgb(originalColor.A, 255 - originalColor.R, 255 - originalColor.G, 255 - originalColor.B);
                    invertedBitmap.SetPixel(x, y, invertedColor);
                }
            }
            return invertedBitmap;
        }

        // 5

        //6
        private Dictionary<string, int[]> CalculateHistogram(Bitmap bitmap)
        {
            int[] redHistogram = new int[256];
            int[] greenHistogram = new int[256];
            int[] blueHistogram = new int[256];

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    redHistogram[pixelColor.R]++;
                    greenHistogram[pixelColor.G]++;
                    blueHistogram[pixelColor.B]++;
                }
            }

            return new Dictionary<string, int[]>
    {
        { "Red", redHistogram },
        { "Green", greenHistogram },
        { "Blue", blueHistogram }
    };
        }

        private void PlotHistogram(Dictionary<string, int[]> histograms)
        {
            Form histogramForm = new Form
            {
                Text = "Color Channel Histograms",
                Size = new Size(800, 600)
            };

            PictureBox pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = new Bitmap(800, 600)
            };

            using (Graphics g = Graphics.FromImage(pictureBox.Image))
            {
                g.Clear(Color.White);

                int[] redHistogram = histograms["Red"];
                int[] greenHistogram = histograms["Green"];
                int[] blueHistogram = histograms["Blue"];

                int maxRed = redHistogram.Max();
                int maxGreen = greenHistogram.Max();
                int maxBlue = blueHistogram.Max();
                int max = Math.Max(maxRed, Math.Max(maxGreen, maxBlue));

                for (int i = 0; i < 256; i++)
                {
                    int redHeight = (int)(redHistogram[i] / (float)max * 200);
                    int greenHeight = (int)(greenHistogram[i] / (float)max * 200);
                    int blueHeight = (int)(blueHistogram[i] / (float)max * 200);

                    g.DrawLine(Pens.Red, i * 3, 250, i * 3, 250 - redHeight);
                    g.DrawLine(Pens.Green, i * 3 + 1, 250, i * 3 + 1, 250 - greenHeight);
                    g.DrawLine(Pens.Blue, i * 3 + 2, 250, i * 3 + 2, 250 - blueHeight);
                }
            }

            histogramForm.Controls.Add(pictureBox);
            histogramForm.ShowDialog();
        }

        private int[] CalculateIntensityHistogram(Bitmap bitmap)
        {
            int[] intensityHistogram = new int[256];

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    int intensity = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                    intensityHistogram[intensity]++;
                }
            }

            return intensityHistogram;
        }
        private void PlotIntensityHistogram(int[] histogram)
        {
            Form histogramForm = new Form
            {
                Text = "Intensity Histogram",
                Size = new Size(800, 600)
            };

            PictureBox pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = new Bitmap(800, 600)
            };

            using (Graphics g = Graphics.FromImage(pictureBox.Image))
            {
                g.Clear(Color.White);

                int max = histogram.Max();

                for (int i = 0; i < 256; i++)
                {
                    int height = (int)(histogram[i] / (float)max * 200);
                    g.DrawLine(Pens.Black, i * 3, 250, i * 3, 250 - height);
                }
            }

            histogramForm.Controls.Add(pictureBox);
            histogramForm.ShowDialog();
        }


        private Bitmap EqualizeHistogram(Bitmap original)
        {
            Bitmap equalizedBitmap = new Bitmap(original.Width, original.Height);

            // Calculate the histogram
            int[] histogram = new int[256];
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int intensity = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                    histogram[intensity]++;
                }
            }

            // Calculate the cumulative distribution function (CDF)
            int[] cdf = new int[256];
            cdf[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + histogram[i];
            }

            // Normalize the CDF
            int cdfMin = cdf.First(c => c > 0);
            int totalPixels = original.Width * original.Height;
            int[] equalizedValues = new int[256];
            for (int i = 0; i < 256; i++)
            {
                equalizedValues[i] = (int)(((float)(cdf[i] - cdfMin) / (totalPixels - cdfMin)) * 255);
            }

            // Apply the equalized values to the bitmap
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int intensity = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                    int equalizedIntensity = equalizedValues[intensity];
                    Color equalizedColor = Color.FromArgb(pixelColor.A, equalizedIntensity, equalizedIntensity, equalizedIntensity);
                    equalizedBitmap.SetPixel(x, y, equalizedColor);
                }
            }

            return equalizedBitmap;
        }

        private Bitmap EqualizeHistogramAllChannels(Bitmap original)
        {
            Bitmap equalizedBitmap = new Bitmap(original.Width, original.Height);

            // Separate histograms for each channel
            int[] redHistogram = new int[256];
            int[] greenHistogram = new int[256];
            int[] blueHistogram = new int[256];

            // Calculate the histograms
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    redHistogram[pixelColor.R]++;
                    greenHistogram[pixelColor.G]++;
                    blueHistogram[pixelColor.B]++;
                }
            }

            // Calculate the cumulative distribution function (CDF) for each channel
            int[] redCdf = new int[256];
            int[] greenCdf = new int[256];
            int[] blueCdf = new int[256];

            redCdf[0] = redHistogram[0];
            greenCdf[0] = greenHistogram[0];
            blueCdf[0] = blueHistogram[0];

            for (int i = 1; i < 256; i++)
            {
                redCdf[i] = redCdf[i - 1] + redHistogram[i];
                greenCdf[i] = greenCdf[i - 1] + greenHistogram[i];
                blueCdf[i] = blueCdf[i - 1] + blueHistogram[i];
            }

            // Normalize the CDFs
            int totalPixels = original.Width * original.Height;
            int[] redEqualizedValues = new int[256];
            int[] greenEqualizedValues = new int[256];
            int[] blueEqualizedValues = new int[256];

            for (int i = 0; i < 256; i++)
            {
                redEqualizedValues[i] = (int)(((float)redCdf[i] / totalPixels) * 255);
                greenEqualizedValues[i] = (int)(((float)greenCdf[i] / totalPixels) * 255);
                blueEqualizedValues[i] = (int)(((float)blueCdf[i] / totalPixels) * 255);
            }

            // Apply the equalized values to the bitmap
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int redEqualized = redEqualizedValues[pixelColor.R];
                    int greenEqualized = greenEqualizedValues[pixelColor.G];
                    int blueEqualized = blueEqualizedValues[pixelColor.B];
                    Color equalizedColor = Color.FromArgb(pixelColor.A, redEqualized, greenEqualized, blueEqualized);
                    equalizedBitmap.SetPixel(x, y, equalizedColor);
                }
            }

            return equalizedBitmap;
        }

        //6

        //7






        // black edges when blurred
        // https://stackoverflow.com/questions/4854839/how-to-use-pre-multiplied-during-image-convolution-to-solve-alpha-bleed-problem
        private Bitmap ApplyConvolutionFilter(Bitmap original, double[,] kernel, float multiplier = 1.0f)
        {
            Bitmap filteredBitmap = new Bitmap(original.Width, original.Height);
            Bitmap premultiplied = new Bitmap(original);

            int kernelWidth = kernel.GetLength(0);
            int kernelHeight = kernel.GetLength(1);
            int kernelWidthRadius = kernelWidth / 2;
            int kernelHeightRadius = kernelHeight / 2;

            if( multiplier == 1.0f)
            {
                for (int y = kernelHeightRadius; y < original.Height - kernelHeightRadius; y++)
                {
                    for (int x = kernelWidthRadius; x < original.Width - kernelWidthRadius; x++)
                    {
                        double red = 0.0, green = 0.0, blue = 0.0;

                        for (int ky = -kernelHeightRadius; ky <= kernelHeightRadius; ky++)
                        {
                            for (int kx = -kernelWidthRadius; kx <= kernelWidthRadius; kx++)
                            {
                                Color pixelColor = original.GetPixel(x + kx, y + ky);


                                double kernelValue = kernel[ky + kernelHeightRadius, kx + kernelWidthRadius];


                                red += pixelColor.R * kernelValue;
                                green += pixelColor.G * kernelValue;
                                blue += pixelColor.B * kernelValue;
                          
                            }
                        }
                        Color pixelColor2 = original.GetPixel(x, y);
                        int r = Math.Min(Math.Max((int)(red), 0), 255);
                        int g = Math.Min(Math.Max((int)green, 0), 255);
                        int b = Math.Min(Math.Max((int)blue, 0), 255);
         
                        filteredBitmap.SetPixel(x, y, Color.FromArgb(pixelColor2.A, r, g, b));


                    }
                }
            }
            else
            {
                Bitmap newbit = new Bitmap(original);

                for (int y = 0; y < original.Height; y++)
                {
                    for (int x = 0; x < original.Width; x++)
                    {
                        Color pixel = original.GetPixel(x, y);

                        newbit.SetPixel(x, y, Color.FromArgb(pixel.A, (int)(pixel.R * (pixel.A / 255.0)), (int)(pixel.G * (pixel.A / 255.0)), (int)(pixel.B * (pixel.A / 255.0))));
                    }
                }
                for (int y = kernelHeightRadius; y < original.Height - kernelHeightRadius; y++)
                {
                    for (int x = kernelWidthRadius; x < original.Width - kernelWidthRadius; x++)
                    {
                        double red = 0.0, green = 0.0, blue = 0.0;



                        for (int ky = -kernelHeightRadius; ky <= kernelHeightRadius; ky++)
                        {
                            for (int kx = -kernelWidthRadius; kx <= kernelWidthRadius; kx++)
                            {
                                Color pixelColor = newbit.GetPixel(x + kx, y + ky);

                                if (pixelColor.A == 0)
                                {

                                    continue;
                                }

                                double kernelValue = kernel[ky + kernelHeightRadius, kx + kernelWidthRadius];


                                red += pixelColor.R * kernelValue;
                                green += pixelColor.G * kernelValue;
                                blue += pixelColor.B * kernelValue;
                                //alpha += pixelColor.A * kernelValue;
                            }
                        }


                        Color pixelColor2 = newbit.GetPixel(x, y);
                        int r = Math.Min(Math.Max((int)((red * (multiplier) / (pixelColor2.A)) * 255), 0), 255);
                        int g = Math.Min(Math.Max((int)((green * (multiplier) / (pixelColor2.A)) * 255), 0), 255);
                        int b = Math.Min(Math.Max((int)((blue * (multiplier) / (pixelColor2.A)) * 255), 0), 255);

                        if (pixelColor2.A == 0)
                        {
                            filteredBitmap.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                        }
                        else
                        {
                            filteredBitmap.SetPixel(x, y, Color.FromArgb(pixelColor2.A, r, g, b));
                        }



                    }
                }

                //filteredBitmap.Save(@"C:\Users\rlly\Desktop\paint\filteredmine2.png", ImageFormat.Png);

                //for (int y = kernelHeightRadius; y < original.Height - kernelHeightRadius; y++)
                //{
                //    for (int x = kernelWidthRadius; x < original.Width - kernelWidthRadius; x++)
                //    {
                //        double red = 0.0, green = 0.0, blue = 0.0, alpha = 0.0;

                //        int countfalse = 1;

                //        for (int ky = -kernelHeightRadius; ky <= kernelHeightRadius; ky++)
                //        {
                //            for (int kx = -kernelWidthRadius; kx <= kernelWidthRadius; kx++)
                //            {
                //                Color pixelColor = original.GetPixel(x + kx, y + ky);

                //                if (pixelColor.A == 0)
                //                {
                //                    countfalse++;
                //                    continue;
                //                }

                //                double kernelValue = kernel[ky + kernelHeightRadius, kx + kernelWidthRadius];


                //                red += pixelColor.R * kernelValue;
                //                green += pixelColor.G * kernelValue;
                //                blue += pixelColor.B * kernelValue;
                //                alpha += pixelColor.A * kernelValue;
                //            }
                //        }

                //        int r = Math.Min(Math.Max((int)(red * (multiplier)), 0), 255);
                //        int g = Math.Min(Math.Max((int)(green * multiplier), 0), 255);
                //        int b = Math.Min(Math.Max((int)(blue * multiplier), 0), 255);
                //        int a = Math.Min(Math.Max((int)(alpha * (multiplier / countfalse)), 0), 255);
                //        filteredBitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));


                //    }
                //}
                //filteredBitmap.Save(@"C:\Users\rlly\Desktop\paint\filteredmine4.png", ImageFormat.Png);

            }


                    return filteredBitmap;
        }




        //7

        private void ShowInPopup(Bitmap bitmap)
        {
            Form popup = new Form
            {
                Text = "Grayscale Image",
                Size = new Size(bitmap.Width, bitmap.Height)
            };

            PictureBox pictureBox = new PictureBox
            {
                Image = bitmap,
                SizeMode = PictureBoxSizeMode.AutoSize,
            };

            popup.Controls.Add(pictureBox);
            popup.ShowDialog();
        }

        // Prewitt Edge Detection Kernels
        // Prewitt Vertical Kernel
        double[,] PrewittVertical = new double[,]
        {
    { 1, 0, -1 },
    { 1, 0, -1 },
    { 1, 0, -1 }
        };

        // Prewitt Horizontal Kernel
        double[,] PrewittHorizontal = new double[,]
        {
    { 1, 1, 1 },
    { 0, 0, 0 },
    { -1, -1, -1 }
        };

        // Sobel Edge Detection Kernels
        // Sobel Vertical Kernel
        double[,] SobelVertical = new double[,]
        {
    { 1, 0, -1 },
    { 2, 0, -2 },
    { 1, 0, -1 }
        };

        // Sobel Horizontal Kernel
        double[,] SobelHorizontal = new double[,]
        {
    { 1, 2, 1 },
    { 0, 0, 0 },
    { -1, -2, -1 }
        };

        // Robinson Edge Detection Kernels
        // Robinson Vertical Kernel (0°)
        double[,] Robinson0 = new double[,]
        {
    { 0, 1, 0 },
    { 1, -4, 1 },
    { 0, 1, 0 }
        };

        // Robinson Horizontal Kernel (90°)
        double[,] Robinson90 = new double[,]
        {
    { 1, 1, 1 },
    { 0, -4, 0 },
    { -1, -1, -1 }
        };

        // Kirsch Edge Detection Kernels
        // Kirsch Vertical Kernel
        double[,] KirschVertical = new double[,]
        {
    { 5, 5, 5 },
    { -3, 0, -3 },
    { -3, -3, -3 }
        };

        // Kirsch Horizontal Kernel
        double[,] KirschHorizontal = new double[,]
        {
    { 5, -3, -3 },
    { 5, 0, -3 },
    { 5, -3, -3 }
        };


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
            ResetModes();
        }
        private void penButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.pen;
            ResetModes();
        }
        private void dragButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.drag;
            ResetModes();
        }
        private void rubberButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.rubber; // done
            ResetModes();
        }
        private void eyeButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.eyedropper;
            ResetModes();
        }
        private void zoomButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.zoom;
            ResetModes();
        }
        private void fontButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.font;
            ResetModes();
        }
        private void selectBoxButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.rectangleSelect;
            ResetModes();
        }
        private void selectFreeButton_MouseClick(object sender, MouseEventArgs e)
        {
            currentMode = Mode.freeSelect;
            ResetModes();
        }

        private void importImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filePath = GetPathFile();
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                int layerid = layerManager.GetNextLayerId();
                AddLayerToLayerPanel(layerid);
                layerManager.AddLayer(layerid, new Bitmap(filePath));
                RedrawCanvas(new Rectangle(0, 0, canvasPanel.Width, canvasPanel.Height));
            }
            else
            {
                MessageBox.Show("Invalid file path. Please select a valid image file.");
            }
        }

        private void pNGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImage(ImageFormat.Png);
        }

        private void jPGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImage(ImageFormat.Jpeg);
        }

        private void bMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImage(ImageFormat.Bmp);
        }



        private void grayscaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap grayscaleBitmap = ConvertToGrayscale(selectedLayer.Bitmap);
                ShowInPopup(grayscaleBitmap);
            }
        }

        private void inverseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap inversedBitmap = InverseBitmap(selectedLayer.Bitmap);
                ShowInPopup(inversedBitmap);
            }
        }

        private void rGBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                PlotHistogram(CalculateHistogram(selectedLayer.Bitmap));
            }
         
        }

        private void intensityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                PlotIntensityHistogram(CalculateIntensityHistogram(selectedLayer.Bitmap));
            }
        }

        private void equalizedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap equalizedBitmap = EqualizeHistogram(selectedLayer.Bitmap);
                ShowInPopup(equalizedBitmap);
            }
        }

        private void equalizedAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap equalizedBitmap = EqualizeHistogramAllChannels(selectedLayer.Bitmap);
                ShowInPopup(equalizedBitmap);
            }
        }
        // convulution
        private void identityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
            {
                { 0, 0, 0 },
                { 0,  1, 0 },
                { 0, 0, 0 }
            };
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel);
                ShowInPopup(filteredBitmap);
            }
        }

    
        private void edgeDetectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
          {
                { -1, -1, -1 },
                { -1,  8, -1 },
                { -1, -1, -1 }
          };
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel);
                ShowInPopup(filteredBitmap);
            }
        }

        private void sharpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // hornofrekvencni filtr
            double[,] kernel = new double[,]
       {
                { 0, -1, 0},
                { -1,  5, -1 },
                { 0, -1, 0 }
       };
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel);
                ShowInPopup(filteredBitmap);
            }
        }

        private void boxBlurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
  {
                { 1, 1, 1},
                { 1,  1, 1 },
                { 1, 1, 1 }
  };
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel,1.0f/9.0f);
                ShowInPopup(filteredBitmap);
            }
        }
        //https://lisyarus.github.io/blog/posts/blur-coefficients-generator.html
        private void x3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
{
                { 1, 2, 1},
                { 2,  4, 2 },
                { 1, 2, 1 }
};
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel, 1.0f / 16.0f);
                ShowInPopup(filteredBitmap);
            }
        }

        private void x5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
{                // binomical same as gauss but gauss runs more times over
                { 1, 4, 6,4,1},
                { 4,16,24,16,4},
                { 6,24,36,24,6},
                {4,16,24,16,4 },
                { 1, 4, 6,4,1}
};
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel, 1.0f / 256.0f);
                ShowInPopup(filteredBitmap);
            }
        }

        private void unsharpMaskingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
{
                { 1, 4, 6,4,1},
                { 4,16,24,16,4},
                { 6,24,-476,24,6},
                {4,16,24,16,4 },
                { 1, 4, 6,4,1}
};
            Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            if (selectedLayer != null && selectedLayer.Bitmap != null)
            {
                Bitmap filteredBitmap = ApplyConvolutionFilter(selectedLayer.Bitmap, kernel, -1.0f / 256.0f);
                ShowInPopup(filteredBitmap);
            }
        }
        
        private void edgeDetectionLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] kernel = new double[,]
{
                  { 1, 0, -1 },
                { 1,  0, -1 },
                { 1, 0, -1 }
};
            //Layer selectedLayer = layerManager.GetLayer(selectedLayerId);
            //if (selectedLayer != null && selectedLayer.Bitmap != null)
            //{
                Bitmap filteredBitmap = ApplyConvolutionFilter(combinedBitmap, kernel);
                ShowInPopup(filteredBitmap);
            //}
        }
    }

}