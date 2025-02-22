using System.Drawing;
using System.Windows.Forms;

namespace Photoapp
{
    // dark menu strip ovveride
    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            // Clear the background before filling it
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(49, 54, 59)), e.Item.ContentRectangle);

            // Check if the item is selected or hovered
            if (e.Item.Selected)
            {
                // Optional: slightly change the background when selected
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(79, 84, 89)), e.Item.ContentRectangle);
                e.Item.ForeColor = Color.White; // Text color for hovered item
            }
            else
            {
                e.Item.ForeColor = Color.White; // Text color for non-hovered items
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Clear the background before filling it
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(49, 54, 59)), e.AffectedBounds);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            // Draw the separator color correctly
            e.Graphics.FillRectangle(Brushes.Gray, e.Item.ContentRectangle);
        }
    }
}