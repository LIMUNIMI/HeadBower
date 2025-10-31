using NITHdmis.Music;
using System;
using HeadBower.Modules;

namespace HeadBower.Settings
{
    [Serializable]
    internal class DefaultSettings : UserSettings
    {
        public DefaultSettings() : base()
        {
            HighlightStrokeDim = 5;
            HighlightRadius = 65;
            VerticalSpacer = -100;
            HorizontalSpacer = 200;
            OccluderOffset = 35;
            EllipseRadius = 23;
            LineThickness = 3;
            SharpNotesMode = _SharpNotesModes.On;
            BlinkSelectScaleMode = _BlinkSelectScaleMode.On;
            ModulationControlMode = _ModulationControlModes.On;
            BowPressureControlMode = _BowPressureControlModes.Off; // DEFAULT: OFF for debugging
            ModulationControlSource = ModulationControlSources.HeadPitch;
            BowPressureControlSource = BowPressureControlSources.HeadPitch;
            PressureControlSource = PressureControlSources.HeadYawVelocity;
            MouthClosedNotePreventionMode = _MouthClosedNotePreventionModes.Off;
            InteractionMapping = InteractionMappings.HeadBow;
            SlidePlayMode = _SlidePlayModes.Off;
            SensorPort = 4;
            MIDIPort = 1;
            RootNote = AbsNotes.C;
            ScaleCode = ScaleCodes.maj;
            NoteNamesVisualized = false;
            SensorIntensityHead = 0.1f;
            HeadTrackingSource = HeadTrackingSources.EyeTracker;
        }
    }
}