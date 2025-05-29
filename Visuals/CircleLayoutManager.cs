using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NITHdmis.Music;
using HeadBower.Modules;
using HeadBower.Surface;

namespace HeadBower.Visuals
{
    /// <summary>
    /// Gestisce il layout circolare dei pulsanti delle note, supportando scala cromatica
    /// con un cerchio esterno per gli accidentali.
    /// </summary>
    public class CircleLayoutManager
    {
        private Canvas _noteCanvas;
        private MappingModule _mappingModule;
        private AutoScroller_ButtonMover _autoScroller;

        // Colori per i diversi tipi di note
        private readonly Brush _naturalNotesBrush = Brushes.SteelBlue;
        private readonly Brush _accidentalNotesBrush = Brushes.DarkGray;
        private readonly Brush _rootNoteBrush = Brushes.DarkOrange;

        // Parametri per il layout circolare
        private double _innerRadius = 180;      // Raggio del cerchio interno (note naturali)
        private double _middleRadius = 250;     // Raggio per le note dell'ottava precedente
        private double _outerRadius = 320;      // Raggio del cerchio esterno (note accidentali)
        private double _buttonSize = 60;        // Dimensione base dei pulsanti
        private double _accidentalButtonSize = 50; // Dimensione dei pulsanti per gli accidentali
        private double _extendedOctaveButtonSize = 55; // Dimensione pulsanti ottava precedente/successiva

        public CircleLayoutManager(Canvas noteCanvas, MappingModule mappingModule)
        {
            _noteCanvas = noteCanvas;
            _mappingModule = mappingModule;
        }

        /// <summary>
        /// Disegna il layout circolare completo con un'ottava e mezzo e note accidentali
        /// </summary>
        public void DrawCircleLayout()
        {
            // Prima puliamo il canvas
            _noteCanvas.Children.Clear();

            // Ottiene la nota di riferimento (C centrale) e la scala
            int baseMidiNote = (int)MidiNotes.C4; // C centrale
            Scale currentScale = _mappingModule.NetytarSurface?.Scale ?? new Scale(AbsNotes.C, ScaleCodes.maj);

            // Calcola il centro del canvas
            Point center = new Point(
                _noteCanvas.ActualWidth / 2,
                _noteCanvas.ActualHeight / 2
            );

            // Disegna il cerchio principale con le note naturali (C4-B4)
            DrawMainOctaveNotes(baseMidiNote, center, currentScale);

            // Disegna il cerchio medio con le note dell'ottava precedente (G3-B3)
            DrawPreviousOctaveNotes(baseMidiNote - 12, center, currentScale);

            // Disegna il cerchio medio con le note dell'ottava successiva (C5-F5)
            DrawNextOctaveNotes(baseMidiNote + 12, center, currentScale);

            // Disegna il cerchio esterno con le note accidentali
            DrawAccidentalNotes(baseMidiNote, center, currentScale);

            // Inizializza l'AutoScroller dopo aver creato tutti i pulsanti
            InitializeAutoScroller();
        }

        /// <summary>
        /// Disegna le note dell'ottava principale (C4-B4) sul cerchio interno
        /// </summary>
        private void DrawMainOctaveNotes(int baseMidiNote, Point center, Scale currentScale)
        {
            // Note naturali in un'ottava: C, D, E, F, G, A, B
            int[] naturalNotes = { 0, 2, 4, 5, 7, 9, 11 };

            double angleStep = 360.0 / naturalNotes.Length;

            // Disegna le note naturali dell'ottava principale
            for (int i = 0; i < naturalNotes.Length; i++)
            {
                int midiNote = baseMidiNote + naturalNotes[i];

                // Calcola l'angolo (partiamo da sopra e andiamo in senso orario)
                double angle = -90 + i * angleStep;
                double radians = angle * Math.PI / 180;

                // Calcola la posizione
                double x = center.X + Math.Cos(radians) * _innerRadius - _buttonSize / 2;
                double y = center.Y + Math.Sin(radians) * _innerRadius - _buttonSize / 2;

                // Crea e aggiunge il pulsante con lo stile RoundButton
                Button noteButton = CreateNoteButton(midiNote, x, y, _buttonSize, currentScale, false);
                _noteCanvas.Children.Add(noteButton);
            }
        }

        /// <summary>
        /// Disegna le note dell'ottava precedente (G3-B3) sul cerchio medio
        /// </summary>
        private void DrawPreviousOctaveNotes(int baseMidiNote, Point center, Scale currentScale)
        {
            // Note naturali in un'ottava: C, D, E, F, G, A, B
            int[] naturalNotes = { 0, 2, 4, 5, 7, 9, 11 };

            // Disegna solo la seconda metà dell'ottava precedente (G3-B3)
            int[] selectedNotes = { 7, 9, 11 }; // G, A, B
            double angleStep = 360.0 / naturalNotes.Length;

            // Disegna le note selezionate
            for (int i = 0; i < selectedNotes.Length; i++)
            {
                int noteIndex = Array.IndexOf(naturalNotes, selectedNotes[i]);
                int midiNote = baseMidiNote + selectedNotes[i];

                // Calcola l'angolo allineandolo con le note della stessa lettera dell'ottava principale
                double angle = -90 + noteIndex * angleStep;
                double radians = angle * Math.PI / 180;

                // Calcola la posizione - metti sul cerchio medio ma più vicino all'esterno
                double x = center.X + Math.Cos(radians) * (_middleRadius - 30) - _extendedOctaveButtonSize / 2;
                double y = center.Y + Math.Sin(radians) * (_middleRadius - 30) - _extendedOctaveButtonSize / 2;

                // Crea e aggiunge il pulsante
                Button noteButton = CreateNoteButton(midiNote, x, y, _extendedOctaveButtonSize, currentScale, false);
                _noteCanvas.Children.Add(noteButton);
            }
        }

        /// <summary>
        /// Disegna le note dell'ottava successiva (C5-F5) sul cerchio medio
        /// </summary>
        private void DrawNextOctaveNotes(int baseMidiNote, Point center, Scale currentScale)
        {
            // Note naturali in un'ottava: C, D, E, F, G, A, B
            int[] naturalNotes = { 0, 2, 4, 5, 7, 9, 11 };

            // Disegna solo la prima metà dell'ottava successiva (C5-F5)
            int[] selectedNotes = { 0, 2, 4, 5 }; // C, D, E, F
            double angleStep = 360.0 / naturalNotes.Length;

            // Disegna le note selezionate
            for (int i = 0; i < selectedNotes.Length; i++)
            {
                int noteIndex = Array.IndexOf(naturalNotes, selectedNotes[i]);
                int midiNote = baseMidiNote + selectedNotes[i];

                // Calcola l'angolo allineandolo con le note della stessa lettera dell'ottava principale
                double angle = -90 + noteIndex * angleStep;
                double radians = angle * Math.PI / 180;

                // Calcola la posizione - metti sul cerchio medio ma più vicino all'interno
                double x = center.X + Math.Cos(radians) * (_middleRadius + 30) - _extendedOctaveButtonSize / 2;
                double y = center.Y + Math.Sin(radians) * (_middleRadius + 30) - _extendedOctaveButtonSize / 2;

                // Crea e aggiunge il pulsante
                Button noteButton = CreateNoteButton(midiNote, x, y, _extendedOctaveButtonSize, currentScale, false);
                _noteCanvas.Children.Add(noteButton);
            }
        }

        /// <summary>
        /// Disegna le note accidentali (diesis/bemolle) sul cerchio esterno
        /// </summary>
        private void DrawAccidentalNotes(int baseMidiNote, Point center, Scale currentScale)
        {
            // Note accidentali in un'ottava: C#, D#, F#, G#, A#
            int[] accidentalNotes = { 1, 3, 6, 8, 10 };
            int[] naturalNotes = { 0, 2, 4, 5, 7, 9, 11 };

            double angleStep = 360.0 / naturalNotes.Length;

            // Disegna le note accidentali dell'ottava principale
            for (int i = 0; i < accidentalNotes.Length; i++)
            {
                int midiNote = baseMidiNote + accidentalNotes[i];

                // Trova le note naturali vicine
                int previousNaturalIndex = -1;
                for (int j = 0; j < naturalNotes.Length; j++)
                {
                    if (naturalNotes[j] > accidentalNotes[i])
                    {
                        previousNaturalIndex = (j == 0) ? naturalNotes.Length - 1 : j - 1;
                        break;
                    }
                }

                if (previousNaturalIndex == -1)
                {
                    previousNaturalIndex = naturalNotes.Length - 1; // Ultimo elemento
                }

                int nextNaturalIndex = (previousNaturalIndex + 1) % naturalNotes.Length;

                // Calcola l'angolo tra le due note naturali
                double prevAngle = -90 + previousNaturalIndex * angleStep;
                double nextAngle = -90 + nextNaturalIndex * angleStep;

                // Posiziona l'accidentale tra le due note naturali ma sul cerchio esterno
                double angle = (prevAngle + nextAngle) / 2;
                double radians = angle * Math.PI / 180;

                // Calcola la posizione
                double x = center.X + Math.Cos(radians) * _outerRadius - _accidentalButtonSize / 2;
                double y = center.Y + Math.Sin(radians) * _outerRadius - _accidentalButtonSize / 2;

                // Crea e aggiunge il pulsante accidentale
                Button noteButton = CreateNoteButton(midiNote, x, y, _accidentalButtonSize, currentScale, isAccidental: true);
                _noteCanvas.Children.Add(noteButton);
            }

            // Aggiungi anche qualche accidentale dell'ottava precedente e successiva
            AddExtendedAccidentals(baseMidiNote - 12, center, currentScale, naturalNotes, angleStep); // Ottava precedente
            AddExtendedAccidentals(baseMidiNote + 12, center, currentScale, naturalNotes, angleStep); // Ottava successiva
        }

        /// <summary>
        /// Aggiunge accidentali selezionati per le ottave estese (precedente e successiva)
        /// </summary>
        private void AddExtendedAccidentals(int baseMidiNote, Point center, Scale currentScale, int[] naturalNotes, double angleStep)
        {
            // Scegli solo alcuni accidentali rilevanti per le ottave estese
            int[] selectedAccidentals = baseMidiNote < 60 ? new int[] { 8, 10 } : new int[] { 1, 3 }; // G#, A# per ottava precedente oppure C#, D# per ottava successiva

            foreach (int accidental in selectedAccidentals)
            {
                int midiNote = baseMidiNote + accidental;

                // Trova le note naturali vicine come nell'ottava principale
                int previousNaturalIndex = -1;
                for (int j = 0; j < naturalNotes.Length; j++)
                {
                    if (naturalNotes[j] > accidental)
                    {
                        previousNaturalIndex = (j == 0) ? naturalNotes.Length - 1 : j - 1;
                        break;
                    }
                }

                if (previousNaturalIndex == -1) previousNaturalIndex = naturalNotes.Length - 1;

                // Calcola l'angolo come per l'ottava principale
                double angle = -90 + previousNaturalIndex * angleStep + angleStep / 2;
                double radians = angle * Math.PI / 180;

                // Posiziona gli accidentali dell'ottava estesa con un offset radiale
                double radius = baseMidiNote < 60 ? _outerRadius - 40 : _outerRadius + 40;
                double x = center.X + Math.Cos(radians) * radius - _accidentalButtonSize / 2;
                double y = center.Y + Math.Sin(radians) * radius - _accidentalButtonSize / 2;

                // Crea un pulsante leggermente più piccolo per gli accidentali estesi
                Button noteButton = CreateNoteButton(midiNote, x, y, _accidentalButtonSize * 0.9, currentScale, isAccidental: true);
                _noteCanvas.Children.Add(noteButton);
            }
        }

        /// <summary>
        /// Crea un pulsante per una nota musicale con lo stile RoundButton originale
        /// </summary>
        private Button CreateNoteButton(int midiNote, double x, double y, double size, Scale currentScale, bool isAccidental = false)
        {
            MidiNotes note = (MidiNotes)midiNote;
            AbsNotes absNote = note.ToAbsNote();

            Button button = new Button();

            // Imposta lo stile RoundButton e mantieni gli stili originali
            button.Style = (Style)Application.Current.Resources["RoundButton"];

            // Imposta dimensioni e posizione
            button.Width = size;
            button.Height = size;
            Canvas.SetLeft(button, x);
            Canvas.SetTop(button, y);

            // Imposta tag con il valore MIDI della nota
            button.Tag = midiNote.ToString();

            // Aggiungiamo una proprietà per identificare il tipo di nota (principale, estesa, accidentale)
            button.SetValue(Button.NameProperty, isAccidental ? "accidental" : "natural");

            // Determina il nome della nota da visualizzare
            string noteText = absNote.ToString();
            if (isAccidental)
            {
                noteText += "#";
            }

            // Aggiungi il numero dell'ottava
            int octave = midiNote / 12 - 1; // Formula standard per calcolare l'ottava MIDI
            noteText += octave.ToString();

            // Imposta il testo del pulsante
            button.Content = noteText;

            // Imposta lo stile e il colore
            button.FontSize = size * 0.35;
            button.FontWeight = FontWeights.Bold;

            // Colora in base al tipo e alla posizione nella scala
            Brush buttonBrush;
            if (isAccidental)
            {
                buttonBrush = _accidentalNotesBrush;
            }
            else if (absNote == currentScale.RootNote)
            {
                buttonBrush = _rootNoteBrush;
            }
            else
            {
                buttonBrush = _naturalNotesBrush;
            }

            button.Background = buttonBrush;

            // Mantieni i gestori eventi originali
            button.MouseEnter += NoteButton_MouseEnter;

            return button;
        }

        /// <summary>
        /// Inizializza l'AutoScroller per gestire il movimento dei pulsanti
        /// </summary>
        private void InitializeAutoScroller()
        {
            // Se l'autoscroller esiste già, lo resettiamo prima di aggiornarlo
            if (_autoScroller != null)
            {
                _autoScroller.ResetToInitialPositions();
                _autoScroller = null;
            }

            // Creazione di un nuovo autoscroller con impostazioni ottimizzate per il layout circolare
            _autoScroller = new AutoScroller_ButtonMover(
                _noteCanvas,
                50,  // radiusThreshold
                100, // proportional
                0.02, // centerMovementFactor
                0.15  // arrangementSpeed
            );

            // Registra nuovamente tutti i pulsanti
            _autoScroller.RegisterButtons();

            // Aggiorna le posizioni iniziali per riferimento futuro
            _autoScroller.UpdateInitialPositions();

            // Abilita l'autoscroller
            _autoScroller.Enabled = true;
        }

        // Metodo per gestire l'evento MouseEnter mantenendo la logica originale
        private void NoteButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Button button = (Button)sender;
            if (int.TryParse(button.Tag.ToString(), out int note))
            {
                _mappingModule.SelectedNote = (MidiNotes)note;
                _mappingModule.LastGazedButton = button;
                _mappingModule.CurrentButton = button;
            }
        }

        /// <summary>
        /// Aggiorna il layout in base alle dimensioni del canvas
        /// </summary>
        public void UpdateLayout()
        {
            // Aggiorna i raggi in base alle dimensioni del canvas
            double minDimension = Math.Min(_noteCanvas.ActualWidth, _noteCanvas.ActualHeight);
            _innerRadius = minDimension * 0.27;
            _middleRadius = minDimension * 0.36;
            _outerRadius = minDimension * 0.45;

            _buttonSize = minDimension * 0.08;
            _accidentalButtonSize = _buttonSize * 0.85;
            _extendedOctaveButtonSize = _buttonSize * 0.9;

            // Ridisegna il layout
            DrawCircleLayout();
        }
    }
}