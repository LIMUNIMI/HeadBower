using ConsoleEmulation;
using HeadBower.Behaviors.HeadBow;
using HeadBower.Settings;
using HeadBower.Surface;
using HeadBower.Surface.ButtonsSettings;
using HeadBower.Surface.ColorCodes;
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
        public static MappingModule MappingModule { get; set; }
        public static RenderingModule RenderingModule { get; set; }

        public static UDPreceiver UDPreceiverPhone { get; set; }
        public static UDPreceiver UDPreceiverWebcam { get; set; }
        public static UDPsender UDPsenderPhone { get; set; }
        public static UDPreceiver UDPreceiverEyeTracker { get; set; }
        public static USBreceiver USBreceiverHeadTracker { get; set; }

        public static NithModule NithModuleHeadTracker { get; set; }
        public static NithModule NithModuleEyeTracker { get; set; }
        public static NithModule NithModuleWebcam { get; set; }
        public static NithModule NithModulePhone { get; set; }
        public static NithSender NithSenderPhone { get; set; }

        public static ConsoleTextToTextBlock ConsoleWriter { get; set; }
        public static MidiModuleNAudio MidiModule { get; set; }

        public static NITHbehavior_HeadViolinBow Behavior_HeadBow { get; set; }

        public const int HORIZONTALSPACING_MAX = 300;
        public const int HORIZONTALSPACING_MIN = 80;

        public static NithPreprocessor_HeadTrackerCalibrator PreprocessorHeadTrackerCalibrator = new();
        public static IButtonsSettings ButtonsSettings { get; set; } = new DefaultButtonSettings();
        public static IColorCode ColorCode { get; set; } = new DefaultColorCode();
        public static KeyboardModuleWPF KeyboardModule { get; set; }
        public static MainWindow InstrumentWindow { get; set; }

        public static bool RaiseClickEvent { get; internal set; } = false;
        public static SavingSystem SavingSystem { get; set; } = new SavingSystem("Settings");

        public static NetytarSettings UserSettings { get; set; } = new DefaultSettings();
        public static NithSensorBehavior_GazeToMouse GazeToMouse { get; set; }

        public static AutoScroller_ButtonMover AutoScroller { get; set; }

        public static NithPreprocessor_HeadTrackerCalibrator EyeTrackerHeadTrackerCalibrator { get; set; }

        public static NithPreprocessor_HeadTrackerCalibrator WebcamHeadTrackerCalibrator { get; set; }

        // Add phone calibrator so HTcal button can calibrate phone like other sensors
        public static NithPreprocessor_HeadTrackerCalibrator PhoneHeadTrackerCalibrator { get; set; }

        public static NithDeviceDiscoveryService NithDeviceDiscoveryService { get; set; }
    }
}