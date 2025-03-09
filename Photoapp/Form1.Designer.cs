using System.Windows.Forms;

namespace Photoapp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        //public class DoubleBufferedPanel : Panel
        //{
        //    public DoubleBufferedPanel()
        //    {
        //        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        //        this.UpdateStyles();
        //    }
        //}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.layerPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.labelLayers = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.selectFreeButton = new FontAwesome.Sharp.IconButton();
            this.selectBoxButton = new FontAwesome.Sharp.IconButton();
            this.fontButton = new FontAwesome.Sharp.IconButton();
            this.zoomButton = new FontAwesome.Sharp.IconButton();
            this.eyeButton = new FontAwesome.Sharp.IconButton();
            this.rubberButton = new FontAwesome.Sharp.IconButton();
            this.dragButton = new FontAwesome.Sharp.IconButton();
            this.penButton = new FontAwesome.Sharp.IconButton();
            this.pencilButton = new FontAwesome.Sharp.IconButton();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.canvasPanel = new System.Windows.Forms.DoubleBufferedPanel();
            this.selectedLayer = new System.Windows.Forms.Label();
            this.legend = new System.Windows.Forms.Label();
            this.saveImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pNGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.jPGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bMPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.buttonPanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // layerPanel
            // 
            this.layerPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.layerPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.layerPanel.Location = new System.Drawing.Point(15, 195);
            this.layerPanel.Margin = new System.Windows.Forms.Padding(0);
            this.layerPanel.Name = "layerPanel";
            this.layerPanel.Size = new System.Drawing.Size(130, 242);
            this.layerPanel.TabIndex = 2;
            // 
            // labelLayers
            // 
            this.labelLayers.AutoSize = true;
            this.labelLayers.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelLayers.ForeColor = System.Drawing.Color.White;
            this.labelLayers.Location = new System.Drawing.Point(12, 179);
            this.labelLayers.Name = "labelLayers";
            this.labelLayers.Size = new System.Drawing.Size(44, 13);
            this.labelLayers.TabIndex = 3;
            this.labelLayers.Text = "Layers";
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.selectFreeButton);
            this.buttonPanel.Controls.Add(this.selectBoxButton);
            this.buttonPanel.Controls.Add(this.fontButton);
            this.buttonPanel.Controls.Add(this.zoomButton);
            this.buttonPanel.Controls.Add(this.eyeButton);
            this.buttonPanel.Controls.Add(this.rubberButton);
            this.buttonPanel.Controls.Add(this.dragButton);
            this.buttonPanel.Controls.Add(this.penButton);
            this.buttonPanel.Controls.Add(this.pencilButton);
            this.buttonPanel.Location = new System.Drawing.Point(16, 27);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(129, 92);
            this.buttonPanel.TabIndex = 4;
            // 
            // selectFreeButton
            // 
            this.selectFreeButton.FlatAppearance.BorderSize = 0;
            this.selectFreeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectFreeButton.IconChar = FontAwesome.Sharp.IconChar.HandPointer;
            this.selectFreeButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.selectFreeButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.selectFreeButton.IconSize = 20;
            this.selectFreeButton.Location = new System.Drawing.Point(4, 64);
            this.selectFreeButton.Name = "selectFreeButton";
            this.selectFreeButton.Size = new System.Drawing.Size(24, 24);
            this.selectFreeButton.TabIndex = 8;
            this.selectFreeButton.UseVisualStyleBackColor = true;
            this.selectFreeButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.selectFreeButton_MouseClick);
            // 
            // selectBoxButton
            // 
            this.selectBoxButton.FlatAppearance.BorderSize = 0;
            this.selectBoxButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectBoxButton.IconChar = FontAwesome.Sharp.IconChar.Square;
            this.selectBoxButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.selectBoxButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.selectBoxButton.IconSize = 20;
            this.selectBoxButton.Location = new System.Drawing.Point(94, 34);
            this.selectBoxButton.Name = "selectBoxButton";
            this.selectBoxButton.Size = new System.Drawing.Size(24, 24);
            this.selectBoxButton.TabIndex = 7;
            this.selectBoxButton.UseVisualStyleBackColor = true;
            this.selectBoxButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.selectBoxButton_MouseClick);
            // 
            // fontButton
            // 
            this.fontButton.FlatAppearance.BorderSize = 0;
            this.fontButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.fontButton.IconChar = FontAwesome.Sharp.IconChar.Font;
            this.fontButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.fontButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.fontButton.IconSize = 20;
            this.fontButton.Location = new System.Drawing.Point(64, 34);
            this.fontButton.Name = "fontButton";
            this.fontButton.Size = new System.Drawing.Size(24, 24);
            this.fontButton.TabIndex = 6;
            this.fontButton.UseVisualStyleBackColor = true;
            this.fontButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.fontButton_MouseClick);
            // 
            // zoomButton
            // 
            this.zoomButton.FlatAppearance.BorderSize = 0;
            this.zoomButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.zoomButton.IconChar = FontAwesome.Sharp.IconChar.MagnifyingGlass;
            this.zoomButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.zoomButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.zoomButton.IconSize = 20;
            this.zoomButton.Location = new System.Drawing.Point(34, 34);
            this.zoomButton.Name = "zoomButton";
            this.zoomButton.Size = new System.Drawing.Size(24, 24);
            this.zoomButton.TabIndex = 5;
            this.zoomButton.UseVisualStyleBackColor = true;
            this.zoomButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.zoomButton_MouseClick);
            // 
            // eyeButton
            // 
            this.eyeButton.FlatAppearance.BorderSize = 0;
            this.eyeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.eyeButton.IconChar = FontAwesome.Sharp.IconChar.EyeDropperEmpty;
            this.eyeButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.eyeButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.eyeButton.IconSize = 20;
            this.eyeButton.Location = new System.Drawing.Point(4, 34);
            this.eyeButton.Name = "eyeButton";
            this.eyeButton.Size = new System.Drawing.Size(24, 24);
            this.eyeButton.TabIndex = 4;
            this.eyeButton.UseVisualStyleBackColor = true;
            this.eyeButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.eyeButton_MouseClick);
            // 
            // rubberButton
            // 
            this.rubberButton.FlatAppearance.BorderSize = 0;
            this.rubberButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rubberButton.IconChar = FontAwesome.Sharp.IconChar.Eraser;
            this.rubberButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.rubberButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.rubberButton.IconSize = 20;
            this.rubberButton.Location = new System.Drawing.Point(94, 4);
            this.rubberButton.Name = "rubberButton";
            this.rubberButton.Size = new System.Drawing.Size(24, 24);
            this.rubberButton.TabIndex = 3;
            this.rubberButton.UseVisualStyleBackColor = true;
            this.rubberButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.rubberButton_MouseClick);
            // 
            // dragButton
            // 
            this.dragButton.FlatAppearance.BorderSize = 0;
            this.dragButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.dragButton.IconChar = FontAwesome.Sharp.IconChar.Arrows;
            this.dragButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.dragButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.dragButton.IconSize = 20;
            this.dragButton.Location = new System.Drawing.Point(64, 4);
            this.dragButton.Name = "dragButton";
            this.dragButton.Size = new System.Drawing.Size(24, 24);
            this.dragButton.TabIndex = 2;
            this.dragButton.UseVisualStyleBackColor = true;
            this.dragButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.dragButton_MouseClick);
            // 
            // penButton
            // 
            this.penButton.FlatAppearance.BorderSize = 0;
            this.penButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.penButton.IconChar = FontAwesome.Sharp.IconChar.PenNib;
            this.penButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.penButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.penButton.IconSize = 20;
            this.penButton.Location = new System.Drawing.Point(34, 4);
            this.penButton.Name = "penButton";
            this.penButton.Size = new System.Drawing.Size(24, 24);
            this.penButton.TabIndex = 1;
            this.penButton.UseVisualStyleBackColor = true;
            this.penButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.penButton_MouseClick);
            // 
            // pencilButton
            // 
            this.pencilButton.FlatAppearance.BorderSize = 0;
            this.pencilButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pencilButton.IconChar = FontAwesome.Sharp.IconChar.Pen;
            this.pencilButton.IconColor = System.Drawing.Color.FromArgb(((int)(((byte)(252)))), ((int)(((byte)(252)))), ((int)(((byte)(252)))));
            this.pencilButton.IconFont = FontAwesome.Sharp.IconFont.Auto;
            this.pencilButton.IconSize = 20;
            this.pencilButton.Location = new System.Drawing.Point(4, 4);
            this.pencilButton.Name = "pencilButton";
            this.pencilButton.Size = new System.Drawing.Size(24, 24);
            this.pencilButton.TabIndex = 0;
            this.pencilButton.UseVisualStyleBackColor = true;
            this.pencilButton.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pencilButton_MouseClick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importImageToolStripMenuItem,
            this.saveImageToolStripMenuItem});
            this.fileToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // importImageToolStripMenuItem
            // 
            this.importImageToolStripMenuItem.Name = "importImageToolStripMenuItem";
            this.importImageToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.importImageToolStripMenuItem.Text = "import image";
            this.importImageToolStripMenuItem.Click += new System.EventHandler(this.importImageToolStripMenuItem_Click);
            // 
            // canvasPanel
            // 
            this.canvasPanel.BackColor = System.Drawing.Color.White;
            this.canvasPanel.Location = new System.Drawing.Point(172, 31);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Size = new System.Drawing.Size(616, 411);
            this.canvasPanel.TabIndex = 1;
            this.canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.canvasPanel_Paint);
            this.canvasPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseDown);
            this.canvasPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseMove);
            this.canvasPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseUp);
            // 
            // selectedLayer
            // 
            this.selectedLayer.AutoSize = true;
            this.selectedLayer.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.selectedLayer.ForeColor = System.Drawing.Color.White;
            this.selectedLayer.Location = new System.Drawing.Point(15, 163);
            this.selectedLayer.Name = "selectedLayer";
            this.selectedLayer.Size = new System.Drawing.Size(41, 13);
            this.selectedLayer.TabIndex = 6;
            this.selectedLayer.Text = "label1";
            // 
            // legend
            // 
            this.legend.AutoSize = true;
            this.legend.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.legend.ForeColor = System.Drawing.Color.White;
            this.legend.Location = new System.Drawing.Point(17, 122);
            this.legend.Name = "legend";
            this.legend.Size = new System.Drawing.Size(41, 13);
            this.legend.TabIndex = 7;
            this.legend.Text = "label1";
            // 
            // saveImageToolStripMenuItem
            // 
            this.saveImageToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pNGToolStripMenuItem,
            this.jPGToolStripMenuItem,
            this.bMPToolStripMenuItem});
            this.saveImageToolStripMenuItem.Name = "saveImageToolStripMenuItem";
            this.saveImageToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveImageToolStripMenuItem.Text = "save image";
            // 
            // pNGToolStripMenuItem
            // 
            this.pNGToolStripMenuItem.Name = "pNGToolStripMenuItem";
            this.pNGToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.pNGToolStripMenuItem.Text = "PNG";
            this.pNGToolStripMenuItem.Click += new System.EventHandler(this.pNGToolStripMenuItem_Click);
            // 
            // jPGToolStripMenuItem
            // 
            this.jPGToolStripMenuItem.Name = "jPGToolStripMenuItem";
            this.jPGToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.jPGToolStripMenuItem.Text = "JPG";
            this.jPGToolStripMenuItem.Click += new System.EventHandler(this.jPGToolStripMenuItem_Click);
            // 
            // bMPToolStripMenuItem
            // 
            this.bMPToolStripMenuItem.Name = "bMPToolStripMenuItem";
            this.bMPToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.bMPToolStripMenuItem.Text = "BMP";
            this.bMPToolStripMenuItem.Click += new System.EventHandler(this.bMPToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(54)))), ((int)(((byte)(59)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.legend);
            this.Controls.Add(this.selectedLayer);
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.labelLayers);
            this.Controls.Add(this.layerPanel);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.canvasPanel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.buttonPanel.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel layerPanel;
        private System.Windows.Forms.Label labelLayers;
        private System.Windows.Forms.Panel buttonPanel;
        private FontAwesome.Sharp.IconButton pencilButton;
        private FontAwesome.Sharp.IconButton penButton;
        private FontAwesome.Sharp.IconButton rubberButton;
        private FontAwesome.Sharp.IconButton dragButton;
        private FontAwesome.Sharp.IconButton eyeButton;
        private FontAwesome.Sharp.IconButton fontButton;
        private FontAwesome.Sharp.IconButton zoomButton;
        private FontAwesome.Sharp.IconButton selectFreeButton;
        private FontAwesome.Sharp.IconButton selectBoxButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importImageToolStripMenuItem;
        private System.Windows.Forms.Label selectedLayer;
        private DoubleBufferedPanel canvasPanel;
        private Label legend;
        private ToolStripMenuItem saveImageToolStripMenuItem;
        private ToolStripMenuItem pNGToolStripMenuItem;
        private ToolStripMenuItem jPGToolStripMenuItem;
        private ToolStripMenuItem bMPToolStripMenuItem;
    }
}

