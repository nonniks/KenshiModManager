using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KenshiModManager.Views
{
    /// <summary>
    /// Adorner that provides visual feedback during drag & drop operations
    /// Shows a ghost image of the dragged item and an insertion indicator
    /// </summary>
    public class DragAdorner : Adorner
    {
        private readonly UIElement _visual;
        private readonly Rectangle _insertionIndicator;
        private Point _position;
        private Point _insertionPosition;
        private bool _showInsertion;

        public DragAdorner(UIElement adornedElement, UIElement visual) : base(adornedElement)
        {
            _visual = visual;
            IsHitTestVisible = false;

            // Create insertion indicator (horizontal line)
            _insertionIndicator = new Rectangle
            {
                Height = 3,
                Fill = new SolidColorBrush(Color.FromRgb(78, 201, 176)), // #4EC9B0 - accent color
                RadiusX = 1.5,
                RadiusY = 1.5,
                Opacity = 0.8
            };
        }

        public Point Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    InvalidateVisual();
                }
            }
        }

        public Point InsertionPosition
        {
            get => _insertionPosition;
            set
            {
                if (_insertionPosition != value)
                {
                    _insertionPosition = value;
                    InvalidateVisual();
                }
            }
        }

        public bool ShowInsertion
        {
            get => _showInsertion;
            set
            {
                if (_showInsertion != value)
                {
                    _showInsertion = value;
                    InvalidateVisual();
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw insertion indicator if active
            if (_showInsertion)
            {
                var width = AdornedElement.RenderSize.Width;
                drawingContext.DrawRectangle(
                    _insertionIndicator.Fill,
                    null,
                    new Rect(_insertionPosition.X, _insertionPosition.Y - 1.5, width, 3));
            }

            // Draw ghost image of dragged item
            if (_visual is FrameworkElement element)
            {
                var brush = new VisualBrush(element)
                {
                    Opacity = 0.6,
                    Stretch = Stretch.None
                };

                var rect = new Rect(_position, new Size(element.ActualWidth, element.ActualHeight));
                drawingContext.DrawRectangle(brush, null, rect);
            }
        }
    }
}
