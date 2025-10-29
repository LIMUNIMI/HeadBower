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

            // Head tracker
            Rack.USBreceiverHeadTracker = new USBreceiver();
            Rack.USBreceiverHeadTracker.MaxSamplesPerSecond = 60; // Limit to 60 Hz
            Rack.NithModuleHeadTracker = new NithModule();
            Rack.NithModuleHeadTracker.MaxQueueSize = 20;
            Rack.NithModuleHeadTracker.OverflowBehavior = QueueOverflowBehavior.DropOldest;
            Rack.USBreceiverHeadTracker.Listeners.Add(Rack.NithModuleHeadTracker);

            // Webcam wrapper
            Rack.NithModuleWebcam = new NithModule();
            Rack.NithModuleWebcam.MaxQueueSize = 15;
            Rack.NithModuleWebcam.OverflowBehavior = QueueOverflowBehavior.DropOldest;
            Rack.NithModuleWebcam.Preprocessors.Add(new NithPreprocessor_WebcamWrapper());
            Rack.NithModuleWebcam.Preprocessors.Add(new NithPreprocessor_MAfilterParams(
                new List<NithParameters>
                {
                    NithParameters.head_pos_yaw,
                    NithParameters.head_pos_pitch,
                    NithParameters.head_pos_roll
                },
                0.5f));
            Rack.WebcamHeadTrackerCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            Rack.NithModuleWebcam.Preprocessors.Add(Rack.WebcamHeadTrackerCalibrator);
            // Calculate velocity first (before smoothing) to match eye tracker behavior
            Rack.NithModuleWebcam.Preprocessors.Add(new NithPreprocessor_HeadAccelerationCalculator(
                filterAlpha: 0.2f,
                accelerationSensitivity: 0.2f));
            Rack.UDPreceiverWebcam = new UDPreceiver(20100);
            Rack.UDPreceiverWebcam.MaxSamplesPerSecond = 30; // Webcam is slower
            Rack.UDPreceiverWebcam.Listeners.Add(Rack.NithModuleWebcam);
            Rack.UDPreceiverWebcam.Connect();

            // Eye tracker
            Rack.GazeToMouse = new NithSensorBehavior_GazeToMouse();

            Rack.NithModuleEyeTracker = new NithModule();
            Rack.NithModuleEyeTracker.MaxQueueSize = 30;
            Rack.NithModuleEyeTracker.OverflowBehavior = QueueOverflowBehavior.DropOldest;
            Rack.NithModuleEyeTracker.SensorBehaviors.Add(Rack.GazeToMouse);
            Rack.NithModuleEyeTracker.SensorBehaviors.Add(new EBBdoubleCloseClick());

            Rack.UDPreceiverEyeTracker = new UDPreceiver(20102); // tobias
            Rack.UDPreceiverEyeTracker.MaxSamplesPerSecond = 100; // Eye tracker is fastest
            Rack.UDPreceiverEyeTracker.Listeners.Add(Rack.NithModuleEyeTracker);
            Rack.NithModuleEyeTracker.Preprocessors.Add(new NithPreprocessor_HeadTrackerCalibrator());
            Rack.NithModuleEyeTracker.Preprocessors.Add(new NithPreprocessor_HeadAccelerationCalculator());
            Rack.EyeTrackerHeadTrackerCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            Rack.NithModuleEyeTracker.Preprocessors.Add(Rack.EyeTrackerHeadTrackerCalibrator);

            Rack.UDPreceiverEyeTracker.Connect();
            
            // Phone receiver
            Rack.UDPreceiverPhone = new UDPreceiver((int)NithWrappersReceiverPorts.NITHphoneWrapper);
            Rack.UDPreceiverPhone.MaxSamplesPerSecond = 60; // Limit to 60 Hz - prevent flooding
            Rack.NithModulePhone = new NithModule();
            // Add phone head-tracker calibrator so UI can calibrate phone head pose like other sensors
            Rack.PhoneHeadTrackerCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            Rack.NithModulePhone.Preprocessors.Add(Rack.PhoneHeadTrackerCalibrator);
            Rack.NithModulePhone.MaxQueueSize = 20;           // Buffer up to 20 samples
            Rack.NithModulePhone.OverflowBehavior = QueueOverflowBehavior.DropOldest; // Drop old samples if overwhelmed
            
            // Phone auto-discovery
            Rack.UDPsenderPhone = new UDPsender(21103, "192.168.178.29");
            
            Rack.UDPreceiverPhone.Listeners.Add(Rack.NithModulePhone);
            Rack.UDPreceiverPhone.Connect();

            // Phone sender
            Rack.NithSenderPhone = new NithSender(10000);
            Rack.NithSenderPhone.PortListeners.Add(Rack.UDPsenderPhone);

            // Device Discovery Service
            Rack.NithDeviceDiscoveryService = new NithDeviceDiscoveryService();
            Rack.NithDeviceDiscoveryService.AddBehavior(new DiscoveryBehavior_NithPhoneWrapper(disposables));


            // Keyboard Module
            Rack.KeyboardModule = new KeyboardModuleWPF(Rack.InstrumentWindow, RawInputCaptureMode.Foreground);
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBemulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBstopEmulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBsimulateBlow());


            // Surface
            Rack.AutoScroller = new AutoScroller_ButtonMover(Rack.InstrumentWindow.NoteCanvas, 0, 50, 0.1, 0.18);

            // Rendering module
            Rack.RenderingModule = new RenderingModule(Rack.InstrumentWindow);
            Rack.RenderingModule.StartRendering();

            // HeadBow behavior
            Rack.Behavior_HeadBow = new NITHbehavior_HeadViolinBow(operationMode: NITHbehavior_HeadViolinBow.WhatDoesPitchRotationDo.Modulation);

            // Disposables
            disposables.Add(Rack.USBreceiverHeadTracker);
            disposables.Add(Rack.UDPreceiverEyeTracker);
            disposables.Add(Rack.UDPreceiverWebcam);
            disposables.Add(Rack.UDPreceiverPhone);
            disposables.Add(Rack.UDPsenderPhone);

            disposables.Add(Rack.NithModuleEyeTracker);
            disposables.Add(Rack.NithModuleHeadTracker);
            disposables.Add(Rack.NithModuleWebcam);
            disposables.Add(Rack.NithModulePhone);
            disposables.Add(Rack.RenderingModule);
            disposables.Add(Rack.MidiModule);
            disposables.Add(Rack.NithDeviceDiscoveryService);

            // Start Device Discovery Service (at the end)
            Rack.NithDeviceDiscoveryService.Start();
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