using NITHdmis.Music;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HeadBower.Modules;

namespace HeadBower.Settings
{
    [Serializable]
    public class NetytarSettings : INotifyPropertyChanged
    {
        private float _sensorIntensityHead;
        private _BlinkSelectScaleMode _blinkSelectScaleMode;
        private int _ellipseRadius;
        private int _highlightRadius;
        private int _highlightStrokeDim;
        private int _horizontalSpacer;
        private InteractionMappings _interactionMapping;
        private int _lineThickness;
        private int _midiPort;
        private _ModulationControlModes _modulationControlMode;
        private ModulationControlSources _modulationControlSource;
        private BowPressureControlSources _bowPressureControlSource;
        private bool _noteNamesVisualized;
        private int _occluderOffset;
        private AbsNotes _rootNote;
        private ScaleCodes _scaleCode;
        private int _sensorPort;
        private _SharpNotesModes _sharpNotesMode;
        private _SlidePlayModes _slidePlayMode;
        private int _verticalSpacer;
        private HeadTrackingSources _headTrackingSource;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public NetytarSettings()
        {
        }

        public NetytarSettings(_BlinkSelectScaleMode blinkSelectScaleMode, int ellipseRadius, int highlightRadius, int highlightStrokeDim, int horizontalSpacer, int lineThickness, int midiPort, _ModulationControlModes modulationControlMode, InteractionMappings interactionMethod, bool noteNamesVisualized, int occluderOffset, AbsNotes rootNote, ScaleCodes scaleCode, int sensorPort, _SharpNotesModes sharpNotesMode, _SlidePlayModes slidePlayMode, int verticalSpacer, float sensorIntensityHead, HeadTrackingSources headTrackingSource)
        {
            BlinkSelectScaleMode = blinkSelectScaleMode;
            EllipseRadius = ellipseRadius;
            HighlightRadius = highlightRadius;
            HighlightStrokeDim = highlightStrokeDim;
            HorizontalSpacer = horizontalSpacer;
            LineThickness = lineThickness;
            MIDIPort = midiPort;
            ModulationControlMode = modulationControlMode;
            InteractionMapping = interactionMethod;
            NoteNamesVisualized = noteNamesVisualized;
            OccluderOffset = occluderOffset;
            RootNote = rootNote;
            ScaleCode = scaleCode;
            SensorPort = sensorPort;
            SharpNotesMode = sharpNotesMode;
            SlidePlayMode = slidePlayMode;
            VerticalSpacer = verticalSpacer;
            SensorIntensityHead = sensorIntensityHead;
            HeadTrackingSource = headTrackingSource;
            
            // Initialize new properties with defaults
            ModulationControlSource = ModulationControlSources.HeadPitch;
            BowPressureControlSource = BowPressureControlSources.HeadPitch;
        }

        public _BlinkSelectScaleMode BlinkSelectScaleMode
        {
            get => _blinkSelectScaleMode;
            set => SetProperty(ref _blinkSelectScaleMode, value);
        }

        public int EllipseRadius
        {
            get => _ellipseRadius;
            set => SetProperty(ref _ellipseRadius, value);
        }

        public int HighlightRadius
        {
            get => _highlightRadius;
            set => SetProperty(ref _highlightRadius, value);
        }

        public int HighlightStrokeDim
        {
            get => _highlightStrokeDim;
            set => SetProperty(ref _highlightStrokeDim, value);
        }

        public int HorizontalSpacer
        {
            get => _horizontalSpacer;
            set => SetProperty(ref _horizontalSpacer, value);
        }

        public InteractionMappings InteractionMapping
        {
            get => _interactionMapping;
            set => SetProperty(ref _interactionMapping, value);
        }

        public int LineThickness
        {
            get => _lineThickness;
            set => SetProperty(ref _lineThickness, value);
        }

        public int MIDIPort
        {
            get => _midiPort;
            set => SetProperty(ref _midiPort, value);
        }

        public _ModulationControlModes ModulationControlMode
        {
            get => _modulationControlMode;
            set => SetProperty(ref _modulationControlMode, value);
        }

        public ModulationControlSources ModulationControlSource
        {
            get => _modulationControlSource;
            set => SetProperty(ref _modulationControlSource, value);
        }

        public BowPressureControlSources BowPressureControlSource
        {
            get => _bowPressureControlSource;
            set => SetProperty(ref _bowPressureControlSource, value);
        }

        public bool NoteNamesVisualized
        {
            get => _noteNamesVisualized;
            set => SetProperty(ref _noteNamesVisualized, value);
        }

        public int OccluderOffset
        {
            get => _occluderOffset;
            set => SetProperty(ref _occluderOffset, value);
        }

        public AbsNotes RootNote
        {
            get => _rootNote;
            set => SetProperty(ref _rootNote, value);
        }

        public ScaleCodes ScaleCode
        {
            get => _scaleCode;
            set => SetProperty(ref _scaleCode, value);
        }

        public float SensorIntensityHead
        {
            get => _sensorIntensityHead;
            set => SetProperty(ref _sensorIntensityHead, Math.Max(value, 0.1f));
        }

        public HeadTrackingSources HeadTrackingSource
        {
            get => _headTrackingSource;
            set => SetProperty(ref _headTrackingSource, value);
        }

        public int SensorPort
        {
            get => _sensorPort;
            set => SetProperty(ref _sensorPort, value);
        }

        public _SharpNotesModes SharpNotesMode
        {
            get => _sharpNotesMode;
            set => SetProperty(ref _sharpNotesMode, value);
        }

        public _SlidePlayModes SlidePlayMode
        {
            get => _slidePlayMode;
            set => SetProperty(ref _slidePlayMode, value);
        }

        public int VerticalSpacer
        {
            get => _verticalSpacer;
            set => SetProperty(ref _verticalSpacer, value);
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);

                // Auto-save settings to JSON file
                if (Rack.SavingSystem != null)
                {
                    Rack.SavingSystem.SaveSettings(this);
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}