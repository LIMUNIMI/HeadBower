using ConsoleEmulation;
using HeadBower.Behaviors.Discovery;
using HeadBower.Behaviors.HeadBow;
using HeadBower.OLD.Behaviors.Eyetracker;
using HeadBower.OLD.Behaviors.Keyboard;
using HeadBower.Surface;
using NITHdmis.Modules.MIDI;
using NITHemulation.Modules.Keyboard;
using NITHemulation.Modules.Mouse;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Nith.Module;
using NITHlibrary.Nith.Preprocessors;
using NITHlibrary.Nith.Wrappers;
using NITHlibrary.Nith.Wrappers.NithWebcamWrapper;
using NITHlibrary.Tools.Ports;
using NITHlibrary.Tools.Ports.Discovery;
using NITHlibrary.Tools.Senders;
using RawInputProcessor;

namespace HeadBower.Modules
{
    public class DefaultSetup
    {
        private bool disposed = false;

        public DefaultSetup(MainWindow window)
        {
            Rack.InstrumentWindow = window;
        }

        private List<IDisposable> disposables = [];

        public void Setup()
        {
            // Console writer
            Rack.ConsoleWriter = new ConsoleTextToTextBlock(Rack.InstrumentWindow.txtConsole, Rack.InstrumentWindow.scrConsole);
            Console.SetOut(Rack.ConsoleWriter);

            // Mapping module
            Rack.MappingModule = new MappingModule();

            // MIDI
            Rack.MidiModule = new MidiModuleNAudio(1, 1);
            Rack.MidiModule.OutDevice = 1;

            // Create UDP/USB receivers for all data sources
            SetupReceivers();
            
            // Create the unified NithModule with parameter selector
            SetupUnifiedModule();
            
            // Setup behaviors
            SetupBehaviors();

            // Keyboard Module
            Rack.KeyboardModule = new KeyboardModuleWPF(Rack.InstrumentWindow, RawInputCaptureMode.Foreground);
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBemulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBstopEmulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBsimulateBlow());

            // Surface
            Rack.AutoScroller = new AutoScroller_ButtonMover(Rack.InstrumentWindow.NoteCanvas, 0, 50, 0.1, 0.18);

            // Rendering module - pass initial overlay canvas (ViolinOverlayViolin)
            Rack.RenderingModule = new RenderingModule(Rack.InstrumentWindow, Rack.InstrumentWindow.ViolinOverlayViolin);
            Rack.RenderingModule.StartRendering();

            // Device Discovery Service
            Rack.NithDeviceDiscoveryService = new NithDeviceDiscoveryService();
            Rack.NithDeviceDiscoveryService.AddBehavior(new DiscoveryBehavior_NithPhoneWrapper(disposables));

            // Disposables
            AddDisposables();

            // Start Device Discovery Service (at the end)
            Rack.NithDeviceDiscoveryService.Start();
        }

        private void SetupReceivers()
        {
            // Webcam receiver (NITHwebcamWrapper)
            Rack.UDPreceiverWebcam = new UDPreceiver(20100);
            Rack.UDPreceiverWebcam.MaxSamplesPerSecond = 50; // Limited for performance
            Rack.UDPreceiverWebcam.Connect();

            // Eye tracker receiver (NITHeyetrackerWrapper)
            Rack.UDPreceiverEyeTracker = new UDPreceiver(20102);
            Rack.UDPreceiverEyeTracker.MaxSamplesPerSecond = 50; // Limited for performance
            Rack.UDPreceiverEyeTracker.Connect();

            // Phone receiver (NITHphoneWrapper)
            Rack.UDPreceiverPhone = new UDPreceiver((int)NithWrappersReceiverPorts.NITHphoneWrapper);
            Rack.UDPreceiverPhone.MaxSamplesPerSecond = 100; // Limited for performance
            Rack.UDPreceiverPhone.Connect();

            // Phone sender (for vibration feedback)
            Rack.UDPsenderPhone = new UDPsender(21103, "192.168.178.29");
            Rack.NithSenderPhone = new NithSender(10000);
            Rack.NithSenderPhone.PortListeners.Add(Rack.UDPsenderPhone);

            // USB receiver (optional head tracker)
            Rack.USBreceiverHeadTracker = new USBreceiver();
            Rack.USBreceiverHeadTracker.MaxSamplesPerSecond = 50;
        }

        private void SetupUnifiedModule()
        {
            // Create the unified module
            Rack.NithModuleUnified = new NithModule();
            Rack.NithModuleUnified.MaxQueueSize = 30;
            Rack.NithModuleUnified.OverflowBehavior = QueueOverflowBehavior.DropOldest;

            // Create parameter selector
            Rack.ParameterSelector = new NithPreprocessor_ParameterSelector();
            
            // Configure selector with DEFAULT rules (will be updated by source selection)
            // Default: Webcam for head tracking
            MappingModule.SelectHeadTrackingSource(HeadTrackingSources.Webcam);

            // Create source normalizer
            Rack.SourceNormalizer = new NithPreprocessor_SourceNormalizer();
            
            // Configure per-source sensitivity multipliers from user settings
            Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHwebcamWrapper", Rack.UserSettings.WebcamSensitivity);
            Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHphoneWrapper", Rack.UserSettings.PhoneSensitivity);
            Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHeyetrackerWrapper", Rack.UserSettings.EyeTrackerSensitivity);

            // Add preprocessors in correct order
            // 1. Parameter selector FIRST - filters by source
            Rack.NithModuleUnified.Preprocessors.Add(Rack.ParameterSelector);

            // 2. Webcam wrapper preprocessor (only affects webcam data that passed through)
            Rack.NithModuleUnified.Preprocessors.Add(new NithPreprocessor_WebcamWrapper());

            // 3. Unified calibrator for head tracking
            Rack.UnifiedHeadTrackerCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            Rack.NithModuleUnified.Preprocessors.Add(Rack.UnifiedHeadTrackerCalibrator);

            // 4. Unified motion calculator - calculates velocity from position, and acceleration from velocity
            // This handles all sources: webcam (pos→vel→acc), phone (vel→acc), eye tracker (pos→vel→acc)
            Rack.NithModuleUnified.Preprocessors.Add(new NithPreprocessor_HeadMotionCalculator(
                filterAlpha: 0.3f,
                velocitySensitivity: 0.5f,
                accelerationSensitivity: 0.5f));

            // 5. Source normalizer - applies per-source sensitivity multipliers AFTER motion calculation
            Rack.NithModuleUnified.Preprocessors.Add(Rack.SourceNormalizer);

            // 6. Smoothing filters
            Rack.NithModuleUnified.Preprocessors.Add(new NithPreprocessor_MAfilterParams(
                new List<NithParameters>
                {
                    NithParameters.head_pos_pitch,
                    NithParameters.mouth_ape
                },
                0.5f));

            // Connect all receivers to unified module
            Rack.UDPreceiverWebcam.Listeners.Add(Rack.NithModuleUnified);
            Rack.UDPreceiverEyeTracker.Listeners.Add(Rack.NithModuleUnified);
            Rack.UDPreceiverPhone.Listeners.Add(Rack.NithModuleUnified);
            Rack.USBreceiverHeadTracker.Listeners.Add(Rack.NithModuleUnified);
        }

        private void SetupBehaviors()
        {
            // Create separated behaviors for head bow control
            Rack.Behavior_BowMotion = new BowMotionBehavior();
            Rack.Behavior_ModulationControl = new ModulationControlBehavior();
            Rack.Behavior_BowPressureControl = new BowPressureControlBehavior();
            Rack.Behavior_HapticFeedback = new HapticFeedbackBehavior();

            // Create visual feedback behavior (replaces BowMotionIndicatorBehavior)
            Rack.Behavior_VisualFeedback = new VisualFeedbackBehavior();

            // Apply initial sensitivity from settings (only to BowMotionBehavior and HapticFeedback)
            Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;
            Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;

            // Subscribe to sensitivity changes
            Rack.UserSettings.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Rack.UserSettings.SensorIntensityHead))
                {
                    Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;
                    Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;
                }
                // Update source normalizer when per-source sensitivity changes
                else if (e.PropertyName == nameof(Rack.UserSettings.WebcamSensitivity))
                {
                    Rack.SourceNormalizer.UpdateAllHeadParametersMultiplier("NITHwebcamWrapper", Rack.UserSettings.WebcamSensitivity);
                }
                else if (e.PropertyName == nameof(Rack.UserSettings.PhoneSensitivity))
                {
                    Rack.SourceNormalizer.UpdateAllHeadParametersMultiplier("NITHphoneWrapper", Rack.UserSettings.PhoneSensitivity);
                }
                else if (e.PropertyName == nameof(Rack.UserSettings.EyeTrackerSensitivity))
                {
                    Rack.SourceNormalizer.UpdateAllHeadParametersMultiplier("NITHeyetrackerWrapper", Rack.UserSettings.EyeTrackerSensitivity);
                }
            };

            // Gaze to mouse behavior
            Rack.GazeToMouse = new NithSensorBehavior_GazeToMouse();

            // Add behaviors to unified module
            // Musical logic first
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowMotion);
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_ModulationControl);
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowPressureControl);
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_HapticFeedback);
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.GazeToMouse);
            Rack.NithModuleUnified.SensorBehaviors.Add(new EBBdoubleCloseClick());
            
            // Visual feedback LAST (always runs, always has final say on visual state)
            Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_VisualFeedback);
        }

        private void AddDisposables()
        {
            disposables.Add(Rack.USBreceiverHeadTracker);
            disposables.Add(Rack.UDPreceiverEyeTracker);
            disposables.Add(Rack.UDPreceiverWebcam);
            disposables.Add(Rack.UDPreceiverPhone);
            disposables.Add(Rack.UDPsenderPhone);

            disposables.Add(Rack.NithModuleUnified); // NEW unified module
            
            disposables.Add(Rack.RenderingModule);
            disposables.Add(Rack.MidiModule);
            disposables.Add(Rack.NithDeviceDiscoveryService);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            // Send MIDI panic before disposing
            try
            {
                Rack.MidiModule?.SetPanic();
            }
            catch
            {
                // Silent failure if MIDI panic fails
            }

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables.Clear();
        }
    }
}