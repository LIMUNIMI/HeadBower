// Visuals\ViolinOverlayManager.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HeadBower.Modules;

namespace HeadBower.Visuals
{
    public class ViolinOverlayManager
    {
        private const double THRESHOLD_MULT_AMOUNT = 1.5;
        public Canvas overlayCanvas;
        private readonly Ellipse bowIndicator;
        private readonly Rectangle pitchIndicator; // Cambiato da Ellipse a Rectangle
        private readonly Line pitchUpperLine;
        private readonly Line pitchLowerLine;

        public ViolinOverlayManager(Canvas overlayCanvas)
        {
            // Imposta il canvas senza sfondo inutile
            this.overlayCanvas = overlayCanvas;
            overlayCanvas.Background = null;

            // Inizializza gli elementi grafici:
            // il pallino dell'arco rimane l'ellipse in bianco
            bowIndicator = FindOrCreateEllipse("BowPositionIndicator", 30, 10, Brushes.White);
            // Sostituisce il pallino del pitch con un rettangolo
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

        public void UpdateOverlay(Button currentButton, MappingModule mapping)
        {
            if (currentButton == null)
            {
                overlayCanvas.Visibility = Visibility.Collapsed;
                return;
            }
            overlayCanvas.Visibility = Visibility.Visible;

            try
            {
                // Ottieni la posizione del pulsante rispetto all'overlayCanvas
                Point btnPosition = currentButton.TransformToVisual(overlayCanvas).Transform(new Point(0, 0));
                double btnWidth = currentButton.ActualWidth;
                double btnHeight = currentButton.ActualHeight;

                // Posiziona l'indicatore dell'arco (bow indicator)
                // In acceleration mode, use HeadYawMotion instead of BowPosition
                double bowDisplayValue;
                if (mapping.UsingAccelerationMode)
                {
                    // Map acceleration/velocity magnitude to horizontal position
                    // Range: motion magnitude typically 0-20, map to -1 to 1
                    double motionMagnitude = Math.Abs(mapping.HeadYawMotion);
                    double normalizedMotion = Math.Min(motionMagnitude / 10.0, 1.0); // Scale to 0-1
                    
                    // Show direction: negative motion = left, positive = right
                    bowDisplayValue = mapping.HeadYawMotion >= 0 ? normalizedMotion : -normalizedMotion;
                }
                else
                {
                    // Use position-based bow position (for velocity-based sensors)
                    bowDisplayValue = mapping.BowPosition;
                }
                
                double bowX = ((bowDisplayValue + 1) / 2) * btnWidth;
                double bowY = btnHeight / 2;
                Canvas.SetLeft(bowIndicator, btnPosition.X + bowX - bowIndicator.Width / 2);
                Canvas.SetTop(bowIndicator, btnPosition.Y + bowY - bowIndicator.Height / 2);

                // Mappatura continua del pitch
                double maxDeviation = 50.0;
                double rawPitch = mapping.HeadPitchPosition;
                double effectiveDeviation = Math.Max(-1.0, Math.Min(1.0, rawPitch / maxDeviation));

                double middle = btnPosition.Y + btnHeight / 2;
                double margin = 5; // margine verticale
                double available = btnHeight / 2 - margin;
                double newPitchY = middle - (effectiveDeviation * available);

                // Posiziona il nuovo rettangolino al centro del pulsante
                double pitchX = btnPosition.X + (btnWidth / 2) - (pitchIndicator.Width / 2);
                Canvas.SetLeft(pitchIndicator, pitchX);
                Canvas.SetTop(pitchIndicator, newPitchY - pitchIndicator.Height / 2);

                // Calcola le posizioni delle linee di soglia (gialle) più distanziate
                double thresholdOffset = (mapping.PitchBendThreshold / maxDeviation) * available * THRESHOLD_MULT_AMOUNT;
                double midY = middle;

                // Linea superiore di soglia
                pitchUpperLine.X1 = btnPosition.X;
                pitchUpperLine.X2 = btnPosition.X + btnWidth;
                pitchUpperLine.Y1 = midY - thresholdOffset;
                pitchUpperLine.Y2 = midY - thresholdOffset;

                // Linea inferiore di soglia
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