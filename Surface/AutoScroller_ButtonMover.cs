using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NITHlibrary.Tools.Filters.PointFilters;

namespace HeadBower.Surface
{
    /// <summary>
    /// Gestisce il movimento dei pulsanti nel canvas, sia per l'animazione hover che per il movimento continuo.
    /// </summary>
    public class AutoScroller_ButtonMover
    {
        #region Proprietà e campi
        private readonly Canvas _canvas;
        private readonly List<ButtonBase> _buttons = new List<ButtonBase>();
        private readonly Dictionary<ButtonBase, string> _buttonTags = new Dictionary<ButtonBase, string>();

        // Configurazione
        private readonly int _radiusThreshold;
        private readonly int _proportional;

        // Posizioni e dimensioni
        private double _canvasCenterX;
        private double _canvasCenterY;
        private readonly Dictionary<ButtonBase, System.Windows.Point> _initialPositions = new Dictionary<ButtonBase, System.Windows.Point>();
        private readonly Dictionary<ButtonBase, System.Windows.Point> _relativePositions = new Dictionary<ButtonBase, System.Windows.Point>();

        // Stato di animazione
        private readonly DispatcherTimer _animationTimer = new DispatcherTimer(DispatcherPriority.Render);
        private readonly DispatcherTimer _hoverCheckTimer = new DispatcherTimer(DispatcherPriority.Input);
        private ButtonBase _currentHoveredButton = null;
        private System.Windows.Point _groupCenter;
        private DateTime _lastHoverUpdate = DateTime.Now;

        // Parametri di animazione
        private double CenterMovementFactor = 0.02;  // Velocità di movimento verso il centro
        private double ArrangementSpeed = 0.15;      // Velocità di arrangiamento intorno al bottone hover

        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
        #endregion

        public AutoScroller_ButtonMover(Canvas canvas, int radiusThreshold, int proportional, double centerMovementFactor, double arrangementSpeed)
        {
            _canvas = canvas;
            _radiusThreshold = radiusThreshold;
            _proportional = proportional;
            CenterMovementFactor = centerMovementFactor;
            ArrangementSpeed = arrangementSpeed;


            // Inizializza le dimensioni del canvas
            UpdateCanvasDimensions();
            _canvas.SizeChanged += (s, e) => UpdateCanvasDimensions();

            // Registra i pulsanti e le loro posizioni iniziali
            RegisterButtons();

            // Calcola la posizione iniziale del gruppo
            UpdateGroupCenter();

            // Inizializza il timer per l'animazione principale
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60fps
            _animationTimer.Tick += AnimationTick;
            _animationTimer.Start();

            // Inizializza il timer per il controllo periodico dell'hover
            _hoverCheckTimer.Interval = TimeSpan.FromMilliseconds(100); // Controlla ogni 100ms
            _hoverCheckTimer.Tick += HoverCheckTick;
            _hoverCheckTimer.Start();

            // Aggiungi handler per il movimento del mouse sul canvas
            _canvas.MouseMove += Canvas_MouseMove;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            _lastHoverUpdate = DateTime.Now;

            // Verifica se il mouse è sopra un bottone
            if (e.OriginalSource is UIElement element)
            {
                ButtonBase hoveredButton = null;

                // Risali nella gerarchia visuale per trovare il bottone
                while (element != null && hoveredButton == null)
                {
                    if (element is ButtonBase btn && _buttons.Contains(btn))
                    {
                        hoveredButton = btn;
                    }
                    element = VisualTreeHelper.GetParent(element) as UIElement;
                }

                if (hoveredButton != _currentHoveredButton)
                {
                    _currentHoveredButton = hoveredButton;
                    if (hoveredButton != null)
                    {
                        UpdateRelativePositions();
                    }
                }
            }
        }

        private void HoverCheckTick(object sender, EventArgs e)
        {
            // Se non c'è stata attività del mouse per un po' e c'è un bottone in hover
            // oppure se il mouse non è più sopra alcun bottone, resetta l'hover
            if (_currentHoveredButton != null)
            {
                // Verifica se il mouse è ancora sul canvas
                System.Windows.Point mousePos = Mouse.GetPosition(_canvas);
                bool isMouseOverCanvas = mousePos.X >= 0 && mousePos.Y >= 0 &&
                                         mousePos.X < _canvas.ActualWidth && mousePos.Y < _canvas.ActualHeight;

                if (!isMouseOverCanvas || !IsMouseOverButton(_currentHoveredButton))
                {
                    TimeSpan elapsed = DateTime.Now - _lastHoverUpdate;
                    // Se sono passati più di 500ms dall'ultimo aggiornamento dell'hover, resetta
                    if (elapsed.TotalMilliseconds > 500)
                    {
                        _currentHoveredButton = null;
                    }
                }
            }
        }

        private bool IsMouseOverButton(ButtonBase button)
        {
            System.Windows.Point mousePos = Mouse.GetPosition(button);
            return mousePos.X >= 0 && mousePos.Y >= 0 &&
                   mousePos.X < button.ActualWidth && mousePos.Y < button.ActualHeight;
        }

        private void UpdateCanvasDimensions()
        {
            _canvasCenterX = _canvas.ActualWidth / 2;
            _canvasCenterY = _canvas.ActualHeight / 2;
        }

        public void RegisterButtons()
        {
            foreach (UIElement element in _canvas.Children)
            {
                if (element is ButtonBase button)
                {
                    _buttons.Add(button);

                    // Registra la posizione iniziale
                    double left = Canvas.GetLeft(button);
                    double top = Canvas.GetTop(button);
                    _initialPositions[button] = new System.Windows.Point(left, top);

                    // Salva la posizione relativa al centro del gruppo
                    _relativePositions[button] = new System.Windows.Point(0, 0); // Inizializzata a zero, sarà aggiornata

                    // Registra il tag se presente
                    if (button.Tag != null)
                    {
                        _buttonTags[button] = button.Tag.ToString();
                    }

                    // Aggiungi gestori eventi per hover
                    if (button is Button btn)
                    {
                        btn.MouseEnter -= Button_MouseEnter;
                        btn.MouseLeave -= Button_MouseLeave;
                        btn.MouseEnter += Button_MouseEnter;
                        btn.MouseLeave += Button_MouseLeave;
                    }
                }
            }
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;

            _lastHoverUpdate = DateTime.Now;

            if (sender is ButtonBase button)
            {
                _currentHoveredButton = button;
                UpdateRelativePositions();
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;

            _lastHoverUpdate = DateTime.Now;

            // Non facciamo nulla qui, lasciamo che sia HoverCheckTick a decidere
            // se resettare _currentHoveredButton in base alla posizione del mouse
        }

        private void UpdateRelativePositions()
        {
            if (_currentHoveredButton == null) return;

            // Calcola le posizioni relative standard attorno al bottone in hover
            Dictionary<string, System.Windows.Point> notePositions = new Dictionary<string, System.Windows.Point>
            {
                { "60", new System.Windows.Point(0, -200) },      // C (alto)
                { "62", new System.Windows.Point(150, -150) },    // D (alto-destra)
                { "64", new System.Windows.Point(200, 0) },       // E (destra)
                { "65", new System.Windows.Point(150, 150) },     // F (basso-destra)
                { "67", new System.Windows.Point(0, 200) },       // G (basso)
                { "69", new System.Windows.Point(-150, 150) },    // A (basso-sinistra)
                { "71", new System.Windows.Point(-200, 0) },      // B (sinistra)
                { "72", new System.Windows.Point(-150, -150) }    // C2 (alto-sinistra)
            };

            // Aggiorna le posizioni relative di tutti i bottoni rispetto a quello in hover
            foreach (ButtonBase button in _buttons)
            {
                if (button == _currentHoveredButton)
                {
                    // Il bottone in hover ha posizione relativa zero (è il centro)
                    _relativePositions[button] = new System.Windows.Point(0, 0);
                }
                else if (_buttonTags.TryGetValue(button, out string tag) && notePositions.TryGetValue(tag, out System.Windows.Point pos))
                {
                    // Posizioni specifiche per i bottoni con tag noto
                    _relativePositions[button] = pos;
                }
                else
                {
                    // Per i bottoni senza tag specifico, mantieni la stessa posizione relativa
                    double diffX = Canvas.GetLeft(button) - Canvas.GetLeft(_currentHoveredButton);
                    double diffY = Canvas.GetTop(button) - Canvas.GetTop(_currentHoveredButton);
                    _relativePositions[button] = new System.Windows.Point(diffX, diffY);
                }
            }
        }

        private void UpdateGroupCenter()
        {
            if (_buttons.Count == 0) return;

            double totalX = 0, totalY = 0;

            foreach (ButtonBase button in _buttons)
            {
                totalX += Canvas.GetLeft(button) + button.ActualWidth / 2;
                totalY += Canvas.GetTop(button) + button.ActualHeight / 2;
            }

            _groupCenter = new System.Windows.Point(
                totalX / _buttons.Count,
                totalY / _buttons.Count
            );
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            if (!_enabled) return;

            try
            {
                // Aggiorna la posizione centrale del gruppo
                UpdateGroupCenter();

                // Calcola il movimento verso il centro del canvas
                double targetX = _canvasCenterX;
                double targetY = _canvasCenterY;

                double moveX = (targetX - _groupCenter.X) * CenterMovementFactor;
                double moveY = (targetY - _groupCenter.Y) * CenterMovementFactor;

                // Per ogni bottone, applica entrambe le regole di movimento
                foreach (ButtonBase button in _buttons)
                {
                    // Posizione attuale del bottone
                    double currentX = Canvas.GetLeft(button);
                    double currentY = Canvas.GetTop(button);

                    // 1. Calcola la posizione target basata sul movimento verso il centro
                    double newX = currentX + moveX;
                    double newY = currentY + moveY;

                    // 2. Se c'è un bottone in hover, calcola anche la posizione relativa
                    if (_currentHoveredButton != null)
                    {
                        // Posizione desiderata relativa al bottone in hover
                        System.Windows.Point relPos = _relativePositions[button];

                        // Posizione del bottone hover
                        double hoverX = Canvas.GetLeft(_currentHoveredButton);
                        double hoverY = Canvas.GetTop(_currentHoveredButton);

                        // Posizione target per il bottone attuale
                        double targetButtonX = hoverX + relPos.X;
                        double targetButtonY = hoverY + relPos.Y;

                        // Interposizione verso la posizione target con easing
                        newX = newX + (targetButtonX - newX) * ArrangementSpeed;
                        newY = newY + (targetButtonY - newY) * ArrangementSpeed;
                    }

                    // Aggiorna la posizione del bottone
                    Canvas.SetLeft(button, newX);
                    Canvas.SetTop(button, newY);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'animazione: {ex.Message}");
            }
        }

        /// <summary>
        /// Reimposta tutti i bottoni alle loro posizioni iniziali
        /// </summary>
        public void ResetToInitialPositions()
        {
            foreach (ButtonBase button in _buttons)
            {
                if (_initialPositions.TryGetValue(button, out System.Windows.Point pos))
                {
                    Canvas.SetLeft(button, pos.X);
                    Canvas.SetTop(button, pos.Y);
                }
            }

            _currentHoveredButton = null;
            UpdateGroupCenter();
        }

        /// <summary>
        /// Aggiorna le posizioni iniziali con quelle correnti
        /// </summary>
        public void UpdateInitialPositions()
        {
            foreach (ButtonBase button in _buttons)
            {
                _initialPositions[button] = new System.Windows.Point(
                    Canvas.GetLeft(button),
                    Canvas.GetTop(button)
                );
            }
        }
    }
}