using NITHdmis.Music;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HeadBower.Modules;

namespace HeadBower.Settings
{
    [Serializable]
    public class UserSettings : INotifyPropertyChanged
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
        private _BowPressureControlModes _bowPressureControlMode = _BowPressureControlModes.Off; // DEFAULT: OFF for debugging
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
        private float _webcamSensitivity = 1f;
        private float _phoneSensitivity = 20f;
        private float _eyeTrackerSensitivity = 1f;
        private float _webcamPitchSensitivity = 1f;
        private float _phonePitchSensitivity = 1f;
        private float _eyeTrackerPitchSensitivity = 1f;
        private double _pitchThreshold = 15.0;
        private double _pitchRange = 50.0;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public UserSettings()
        {
            // Initialize new properties with defaults
            ModulationControlSource = ModulationControlSources.HeadPitch;
            BowPressureControlSource = BowPressureControlSources.HeadPitch;
            BowPressureControlMode = _BowPressureControlModes.Off; // DEFAULT: OFF for debugging
        }

        public UserSettings(
            _BlinkSelectScaleMode blinkSelectScaleMode, 
            int ellipseRadius, 
            int highlightRadius, 
            int highlightStrokeDim, 
            int horizontalSpacer, 
            int lineThickness, 
            int midiPort, 
            _ModulationControlModes modulationControlMode, 
            InteractionMappings interactionMethod, 
            bool noteNamesVisualized, 
            int occluderOffset, 
            AbsNotes rootNote, 
            ScaleCodes scaleCode, 
            int sensorPort, 
            _SharpNotesModes sharpNotesMode, 
            _SlidePlayModes slidePlayMode, 
            int verticalSpacer, 
            float sensorIntensityHead, 
            HeadTrackingSources headTrackingSource,
            float webcamSensitivity = 1.0f,
            float phoneSensitivity = 0.3f,
            float eyeTrackerSensitivity = 2.0f)
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
            WebcamSensitivity = webcamSensitivity;
            PhoneSensitivity = phoneSensitivity;
            EyeTrackerSensitivity = eyeTrackerSensitivity;
            
            // Initialize new properties with defaults
            ModulationControlSource = ModulationControlSources.HeadPitch;
            BowPressureControlSource = BowPressureControlSources.HeadPitch;
            BowPressureControlMode = _BowPressureControlModes.Off; // DEFAULT: OFF for debugging
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

        public _BowPressureControlModes BowPressureControlMode
        {
            get => _bowPressureControlMode;
            set => SetProperty(ref _bowPressureControlMode, value);
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

        /// <summary>
        /// Sensitivity multiplier for webcam head tracking data.
        /// Applied to all head parameters (position, velocity, acceleration) from the webcam.
        /// </summary>
        public float WebcamSensitivity
        {
            get => _webcamSensitivity;
            set => SetProperty(ref _webcamSensitivity, Math.Max(value, 0.01f));
        }

        /// <summary>
        /// Sensitivity multiplier for phone head tracking data.
        /// Applied to all head parameters (position, velocity, acceleration) from the phone.
        /// </summary>
        public float PhoneSensitivity
        {
            get => _phoneSensitivity;
            set => SetProperty(ref _phoneSensitivity, Math.Max(value, 0.01f));
        }

        /// <summary>
        /// Sensitivity multiplier for eye tracker head tracking data.
        /// Applied to all head parameters (position, velocity, acceleration) from the eye tracker.
        /// </summary>
        public float EyeTrackerSensitivity
        {
            get => _eyeTrackerSensitivity;
            set => SetProperty(ref _eyeTrackerSensitivity, Math.Max(value, 0.01f));
        }

        /// <summary>
        /// Pitch sensitivity multiplier for webcam head tracking.
        /// Applied specifically to head_pos_pitch parameter from the webcam.
        /// Independent from general head tracking sensitivity.
        /// </summary>
        public float WebcamPitchSensitivity
        {
            get => _webcamPitchSensitivity;
            set => SetProperty(ref _webcamPitchSensitivity, Math.Max(value, 0.01f));
        }

        /// <summary>
        /// Pitch sensitivity multiplier for phone head tracking.
        /// Applied specifically to head_pos_pitch parameter from the phone.
        /// Independent from general head tracking sensitivity.
        /// </summary>
        public float PhonePitchSensitivity
        {
            get => _phonePitchSensitivity;
            set => SetProperty(ref _phonePitchSensitivity, Math.Max(value, 0.01f));
        }

        /// <summary>
        /// Pitch sensitivity multiplier for eye tracker head tracking.
        /// Applied specifically to head_pos_pitch parameter from the eye tracker.
        /// Independent from general head tracking sensitivity.
        /// </summary>
        public float EyeTrackerPitchSensitivity
        {
            get => _eyeTrackerPitchSensitivity;
            set => SetProperty(ref _eyeTrackerPitchSensitivity, Math.Max(value, 0.01f));
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

        /// <summary>
        /// Pitch threshold for visual feedback yellow lines and musical control.
        /// Values below this threshold don't trigger modulation/bow pressure.
        /// </summary>
        public double PitchThreshold
        {
            get => _pitchThreshold;
            set => SetProperty(ref _pitchThreshold, Math.Max(value, 0.0));
        }

        /// <summary>
        /// Maximum expected pitch deviation range for visual feedback normalization.
        /// Used to scale the red rectangle position to fit within the button height.
        /// </summary>
        public double PitchRange
        {
            get => _pitchRange;
            set => SetProperty(ref _pitchRange, Math.Max(value, 1.0));
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