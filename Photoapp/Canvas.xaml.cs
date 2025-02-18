using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Photoapp
{
    /// <summary>
    /// Interaction logic for Canvas.xaml
    /// </summary>
    public partial class DrawingCanvas : UserControl
    {
        private bool isDrawing = false;
        private Point lastPoint;

        public DrawingCanvas()
        {
            InitializeComponent();
        }

        private void MyCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isDrawing = true;
                lastPoint = e.GetPosition(MyCanvas);
            }
        }

        private void MyCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                Point currentPoint = e.GetPosition(MyCanvas);
                // Draw a line between lastPoint and currentPoint
                Line line = new Line
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    X1 = lastPoint.X,
                    Y1 = lastPoint.Y,
                    X2 = currentPoint.X,
                    Y2 = currentPoint.Y
                };
                MyCanvas.Children.Add(line);
                lastPoint = currentPoint;
            }
        }

        private void MyCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
        }
    }
}
