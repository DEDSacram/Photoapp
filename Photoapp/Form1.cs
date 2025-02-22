﻿using System;
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


namespace Photoapp
{

    // UNIONIZING
    // REMOVING
    public partial class Form1 : Form
    {

        private int selectedLayerId = 1; // Default to -1, indicating no layer is selected initially

        private bool isDrawing = false;
        private Point lastPoint;
        private Mode currentMode = Mode.pencil; // Start in drawing mode
        private List<Point> points = new List<Point>(); // Store points for freehand drawing

        private Bitmap virtualCanvas; // for double buffering

        // previous bitmap of UI layer save then draw new one compare if SHIFT OR CTRL WAS HELD DOWN

        private Bitmap UILayer;
        private Bitmap LastUILayer;

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
        // function Modes
        public enum Mode
        {
            pencil, // Free drawing mode
            pen,
            rubber, // Erase mode (optional for future extension)
            drag,
            eyedropper,
            zoom,
            font,
            rectangleSelect,
            freeSelect
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

        public Form1()
        {

            InitializeComponent();

            combinedBitmap = new Bitmap(canvasPanel.Width, canvasPanel.Height);

            // border
            this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizin
            this.Text = "FoxToes";
            menuStrip1.Renderer = new CustomMenuRenderer();

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

        private Point ClampPoint(Point p)
        {
            int x = Math.Max(0, Math.Min(canvasPanel.Width - 1, p.X));
            int y = Math.Max(0, Math.Min(canvasPanel.Height - 1, p.Y));
            return new Point(x, y);
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
        private void RedrawCanvas()
        {
            buildCombinedBitmap();
            canvasPanel.Invalidate();
        }
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
                        points.Add(lastPoint); // Start collecting points for the freehand selection
                        break;

                    case Mode.rectangleSelect:
                        clearUIBitmap();
                        selectionStartPoint = e.Location; // Starting point for the rectangle
                        break;
                }
            }
        }
        // regional edits would be great to avoid checking whole bitmap
        private void canvasPanel_MouseMove(object sender, MouseEventArgs e)
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
                            g.DrawLine(Pens.Black, lastPoint, e.Location); // Draw directly on the layer's bitmap
                        }
                        points.Add(e.Location);
                        lastPoint = e.Location;
                        break;

                    case Mode.rubber:
                        using (Graphics g = Graphics.FromImage(selectedLayer.Bitmap))
                        using (SolidBrush transparentBrush = new SolidBrush(Color.Transparent))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillEllipse(transparentBrush, e.X - 10, e.Y - 10, 20, 20);
                        }
                        break;
                    case Mode.freeSelect:
                        Point clampedPoint = ClampPoint(e.Location);

                        // Draw freehand selection on the UILayer
                        using (Graphics g = Graphics.FromImage(UILayer))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            if (points.Count > 1)
                            {
                                g.DrawLine(Pens.Red, points[points.Count - 2], points.Last());
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
                RedrawCanvas();
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
                        selectionLastPoint = ClampPoint(e.Location);

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

                    UILayer = new Bitmap(MaskControl.MergeAndClearEdges(UILayer, LastUILayer, Color.Blue));
                }
                else
                {
                    LastUILayer = new Bitmap(UILayer);

                }

                lastPoint = e.Location;
                isDrawing = false;

                // Update the combined bitmap and refresh the canvas
                RedrawCanvas();
            }
        }

        private void buildCombinedBitmap()
        {
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                // Ensure transparency blending
                g.CompositingMode = CompositingMode.SourceOver;
                g.Clear(Color.Transparent); // Clear with transparency to support layer blending

                // Draw all layers except the UI layer
                foreach (var layer in layerManager.GetLayers())
                {
                    if (layer.Bitmap != null)
                    {
                        g.DrawImage(layer.Bitmap, 0, 0); // Draw each layer at (0, 0)
                    }
                }

                // Now draw the UI layer on top
                if (UILayer != null)
                {
                    g.DrawImage(UILayer, 0, 0); // Draw the UI layer on top
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