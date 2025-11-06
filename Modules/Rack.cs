using ConsoleEmulation;
using HeadBower.Behaviors.HeadBow;
using HeadBower.Settings;
using HeadBower.Surface;
using HeadBower.Visuals;
using NITHdmis.Modules.MIDI;
using NITHemulation.Modules.Keyboard;
using NITHemulation.Modules.Mouse;
using NITHlibrary.Nith.Module;
using NITHlibrary.Nith.Preprocessors;
using NITHlibrary.Tools.Ports;
using NITHlibrary.Tools.Ports.Discovery;
using NITHlibrary.Tools.Senders;

namespace HeadBower.Modules
{
    internal static class Rack
    {
        // Core modules
        public static MappingModule MappingModule { get; set; }
        public static RenderingModule RenderingModule { get; set; }
        public static MainWindow InstrumentWindow { get; set; }

        // Network receivers
        public static UDPreceiver UDPreceiverPhone { get; set; }
        public static UDPreceiver UDPreceiverWebcam { get; set; }
        public static UDPreceiver UDPreceiverEyeTracker { get; set; }
        public static USBreceiver USBreceiverHeadTracker { get; set; }
        
        // Network senders
        public static UDPsender UDPsenderPhone { get; set; }
        public static NithSender NithSenderPhone { get; set; }
        public static NithDeviceDiscoveryService NithDeviceDiscoveryService { get; set; }

        // Unified sensor architecture
        public static NithModule NithModuleUnified { get; set; }
        public static NithPreprocessor_ParameterSelector ParameterSelector { get; set; }
        public static NithPreprocessor_HeadTrackerCalibrator UnifiedHeadTrackerCalibrator { get; set; }
        public static NithPreprocessor_SourceNormalizer SourceNormalizer { get; set; }
        public static NithPreprocessor_HeadMotionCalculator HeadMotionCalculator { get; set; }
        public static NithPreprocessor_MAfilterParams YawSmoothingFilter { get; set; }

        // Behaviors
        public static BowMotionBehavior Behavior_BowMotion { get; set; }
        public static ModulationControlBehavior Behavior_ModulationControl { get; set; }
        public static BowPressureControlBehavior Behavior_BowPressureControl { get; set; }
        public static HapticFeedbackBehavior Behavior_HapticFeedback { get; set; }
        public static MouthClosedNotePreventionBehavior Behavior_MouthClosedNotePrevention { get; set; }
        public static VisualFeedbackBehavior Behavior_VisualFeedback { get; set; }
        public static NithSensorBehavior_GazeToMouse GazeToMouse { get; set; }

        // Legacy / deprecated
        [Obsolete("Use the separated behaviors (Behavior_BowMotion, Behavior_ModulationControl, etc.) instead")]
        public static NITHbehavior_HeadViolinBow Behavior_HeadBow { get; set; }
        [Obsolete("Use Behavior_VisualFeedback instead - visual feedback is now unified")]
        public static BowMotionIndicatorBehavior Behavior_BowMotionIndicator { get; set; }

        // MIDI
        public static MidiModuleNAudio MidiModule { get; set; }

        // Input modules
        public static KeyboardModuleWPF KeyboardModule { get; set; }

        // UI components
        public static AutoScroller_ButtonMover AutoScroller { get; set; }
        public static ConsoleTextToTextBlock ConsoleWriter { get; set; }
        public static ViolinOverlayState ViolinOverlayState { get; set; } = new ViolinOverlayState();

        // Settings
        public static SavingSystem SavingSystem { get; set; } = new SavingSystem("Settings");
        public static UserSettings UserSettings { get; set; } = new DefaultSettings();

        // Constants
        public const int HORIZONTALSPACING_MAX = 300;
        public const int HORIZONTALSPACING_MIN = 80;

        // Runtime flags
        public static bool RaiseClickEvent { get; internal set; } = false;
    }
}