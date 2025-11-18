namespace HeadBower.Modules
{

    public enum InteractionMappings
    {
        HeadBow
    }

    public enum HeadTrackingSources
    {
        EyeTracker,
        Webcam,
        Phone,
        NITHheadTracker
    }

    public enum _ModulationControlModes
    {
        On,
        Off
    }

    public enum _BowPressureControlModes
    {
        On,
        Off
    }

    public enum ModulationControlSources
    {
        HeadPitch,
        MouthAperture,
        BreathPressure,
        TeethPressure
    }

    public enum BowPressureControlSources
    {
        HeadPitch,
        MouthAperture,
        BreathPressure,
        TeethPressure
    }

    public enum PressureControlSources
    {
        HeadYawVelocity,
        MouthAperture
    }

    public enum _SharpNotesModes
    {
        On,
        Off
    }

    public enum _BlinkSelectScaleMode
    {
        On,
        Off
    }

    public enum _SlidePlayModes
    {
        On,
        Off
    }

    public enum _MouthClosedNotePreventionModes
    {
        On,
        Off
    }
}
