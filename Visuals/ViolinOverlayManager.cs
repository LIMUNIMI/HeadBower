// Visuals\ViolinOverlayManager.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HeadBower.Visuals
{
    public class ViolinOverlayManager
    {
        private const double THRESHOLD_MULT_AMOUNT = 1.5;
        public Canvas overlayCanvas;
        private readonly Ellipse bowIndicator;
        private readonly Rectangle pitchIndicator;
        private readonly Line pitchUpperLine;
        private readonly Line pitchLowerLine;

        public ViolinOverlayManager(Canvas overlayCanvas)
        {
            this.overlayCanvas = overlayCanvas;
            overlayCanvas.Background = null;

            bowIndicator = FindOrCreateEllipse("BowPositionIndicator", 30, 10, Brushes.White);
            pitchIndicator = FindOrCreateRectangle("PitchPositionIndicator", 30, 5, Brushes.Red);
            pitchUpperLine = FindOrCreateLine("PitchBendUpperThreshold", Brushes.Yellow, 2);
            pitchLowerLine = FindOrCreateLine("PitchBendLowerThreshold", Brushes.Yellow, 2);
        }

        private Ellipse FindOrCreateEllipse(string name, double width, double height, Brush fill)
        {
            Ellipse ellipse = null;
            foreach (UIElement child in overlayCanvas.Children)
            {
                if (child is Ellipse el && el.Name == name)
                {
                    ellipse = el;
                    break;
                }
            }
            if (ellipse == null)
            {
                ellipse = new Ellipse { Name = name, Width = width, Height = height, Fill = fill };
                overlayCanvas.Children.Add(ellipse);
            }
            else
            {
                ellipse.Fill = fill;
            }
            return ellipse;
        }

        private Rectangle FindOrCreateRectangle(string name, double width, double height, Brush fill)
        {
            Rectangle rect = null;
            foreach (UIElement child in overlayCanvas.Children)
            {
                if (child is Rectangle r && r.Name == name)
                {
                    rect = r;
                    break;
                }
            }
            if (rect == null)
            {
                rect = new Rectangle { Name = name, Width = width, Height = height, Fill = fill };
                overlayCanvas.Children.Add(rect);
            }
            else
            {
                rect.Fill = fill;
            }
            return rect;
        }

        private Line FindOrCreateLine(string name, Brush stroke, double thickness)
        {
            Line line = null;
            foreach (UIElement child in overlayCanvas.Children)
            {
                if (child is Line ln && ln.Name == name)
                {
                    line = ln;
                    break;
                }
            }
            if (line == null)
            {
                line = new Line { Name = name, Stroke = stroke, StrokeThickness = thickness };
                overlayCanvas.Children.Add(line);
            }
            else
            {
                line.Stroke = stroke;
            }
            return line;
        }

        /// <summary>
        /// Updates the overlay visualization with pre-calculated positioning values.
        /// This method is pure rendering - it only positions elements based on the provided values.
        /// </summary>
        /// <param name="currentButton">The button where the overlay is positioned. If null, hides the overlay.</param>
        /// <param name="bowPosition">Normalized bow position (-1 to 1). Horizontal position of the bow indicator on the button.</param>
        /// <param name="pitchPosition">Normalized pitch position (-1 to 1). Vertical position of the pitch indicator.</param>
        /// <param name="pitchThreshold">Normalized pitch threshold value (0 to 1). Determines where the yellow threshold lines are drawn.</param>
        public void UpdateOverlay(Button currentButton, double bowPosition, double pitchPosition, double pitchThreshold)
        {
            if (currentButton == null)
            {
                overlayCanvas.Visibility = Visibility.Collapsed;
                return;
            }
            overlayCanvas.Visibility = Visibility.Visible;

            try
            {
                Point btnPosition = currentButton.TransformToVisual(overlayCanvas).Transform(new Point(0, 0));
                double btnWidth = currentButton.ActualWidth;
                double btnHeight = currentButton.ActualHeight;

                // Position bow indicator (white ellipse) horizontally
                // bowPosition is already normalized -1 to +1
                double bowX = ((bowPosition + 1) / 2) * btnWidth;
                double bowY = btnHeight / 2;
                Canvas.SetLeft(bowIndicator, btnPosition.X + bowX - bowIndicator.Width / 2);
                Canvas.SetTop(bowIndicator, btnPosition.Y + bowY - bowIndicator.Height / 2);

                // Position pitch indicator (red rectangle) vertically
                // pitchPosition is already normalized -1 to +1
                double middle = btnPosition.Y + btnHeight / 2;
                double margin = 5;
                double available = btnHeight / 2 - margin;
                double newPitchY = middle - (pitchPosition * available);

                double pitchX = btnPosition.X + (btnWidth / 2) - (pitchIndicator.Width / 2);
                Canvas.SetLeft(pitchIndicator, pitchX);
                Canvas.SetTop(pitchIndicator, newPitchY - pitchIndicator.Height / 2);

                // Position threshold lines (yellow)
                // pitchThreshold is already normalized 0 to 1
                double thresholdOffset = pitchThreshold * available * THRESHOLD_MULT_AMOUNT;
                double midY = middle;

                pitchUpperLine.X1 = btnPosition.X;
                pitchUpperLine.X2 = btnPosition.X + btnWidth;
                pitchUpperLine.Y1 = midY - thresholdOffset;
                pitchUpperLine.Y2 = midY - thresholdOffset;

                pitchLowerLine.X1 = btnPosition.X;
                pitchLowerLine.X2 = btnPosition.X + btnWidth;
                pitchLowerLine.Y1 = midY + thresholdOffset;
                pitchLowerLine.Y2 = midY + thresholdOffset;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating violin overlay: {ex.Message}");
            }
        }
    }
}