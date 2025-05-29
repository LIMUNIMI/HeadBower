using ConsoleEmulation;
using HeadBower.Behaviors;
using HeadBower.OLD.Behaviors;
using HeadBower.OLD.Behaviors.Eyetracker;
using HeadBower.OLD.Behaviors.Keyboard;
using HeadBower.Surface;
using NITHdmis.Modules.MIDI;
using NITHdmis.Music;
using NITHemulation.Modules.Keyboard;
using NITHemulation.Modules.Mouse;
using NITHlibrary.Nith.Module;
using NITHlibrary.Nith.Preprocessors;
using NITHlibrary.Nith.Wrappers.NithWebcamWrapper;
using NITHlibrary.Tools.Filters.PointFilters;
using NITHlibrary.Tools.Ports;
using NITHlibrary.Tools.Senders;
using RawInputProcessor;
using System.Windows.Interop;

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
            Rack.ConsoleWriter = new ConsoleTextToTextBox(Rack.InstrumentWindow.txtConsole, Rack.InstrumentWindow.scrConsole);
            Console.SetOut(Rack.ConsoleWriter);

            // Mapping module
            Rack.MappingModule = new MappingModule();

            // MIDI
            Rack.MidiModule = new MidiModuleNAudio(1, 1);
            Rack.MidiModule.OutDevice = 1;

            // Head tracker
            Rack.USBreceiverHeadTracker = new USBreceiver();
            Rack.NithModuleHeadTracker = new NithModule();
            Rack.USBreceiverHeadTracker.Listeners.Add(Rack.NithModuleHeadTracker);

            // Webcam wrapper
            Rack.NithModuleWebcam = new NithModule();
            Rack.NithModuleWebcam.Preprocessors.Add(new NithPreprocessor_WebcamWrapper());

            Rack.UDPreceiverWebcam = new UDPreceiver(20100);
            Rack.UDPreceiverWebcam.Listeners.Add(Rack.NithModuleWebcam);
            Rack.NithModuleWebcam.SensorBehaviors.Add(new WriteToConsoleBehavior());
            Rack.UDPreceiverWebcam.Connect();


            // Eye tracker
            Rack.GazeToMouse = new NithSensorBehavior_GazeToMouse();

            Rack.NithModuleEyeTracker = new NithModule();
            Rack.NithModuleEyeTracker.SensorBehaviors.Add(Rack.GazeToMouse);
            Rack.NithModuleEyeTracker.SensorBehaviors.Add(new EBBdoubleCloseClick());

            Rack.UDPreceiverEyeTracker = new UDPreceiver(20102); // tobias
            Rack.UDPreceiverEyeTracker.Listeners.Add(Rack.NithModuleEyeTracker);
            Rack.NithModuleEyeTracker.Preprocessors.Add(new NithPreprocessor_HeadVelocityCalculator());
            Rack.ETheadTrackerCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            Rack.NithModuleEyeTracker.Preprocessors.Add(Rack.ETheadTrackerCalibrator);

            Rack.UDPreceiverEyeTracker.Connect();
            //Rack.NithModuleEyeTracker.SensorBehaviors.Add(new EBBselectScale(Rack.InstrumentWindow));
            //Rack.NithModuleEyeTracker.SensorBehaviors.Add(new EBBrepeatNote());
            


            // Phone receiver
            Rack.UDPreceiverPhone = new UDPreceiver(20103);
            Rack.NithModulePhone = new NithModule();
            Rack.UDPreceiverPhone.Listeners.Add(Rack.NithModuleHeadTracker);
            Rack.UDPreceiverPhone.Connect();

            // Phone sender
            Rack.UDPsenderPhone = new UDPsender(21103, "192.168.178.29");
            Rack.NithSenderPhone = new NithSender(10000);
            Rack.NithSenderPhone.PortListeners.Add(Rack.UDPsenderPhone);

            // Keyboard Module
            Rack.KeyboardModule = new KeyboardModuleWPF(Rack.InstrumentWindow, RawInputCaptureMode.Foreground);
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBautoScroller());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBemulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBstopAutoScroller());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBstopEmulateMouse());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBsimulateBlow());
            Rack.KeyboardModule.KeyboardBehaviors.Add(new KBselectScale());


            // Surface
            Rack.AutoScroller = new AutoScroller_ButtonMover(Rack.InstrumentWindow.NoteCanvas, 0, 50, 0.1, 0.18);
            // R.NetytarDmiBox.AutoScroller = new Autoscroller_ButtonFollower(R.NetytarMainWindow.scrlNetytar, 0, 130, new PointFilterMAExpDecaying(0.07f)); // OLD was 100, 0.1f
            //Rack.NetytarDmiBox.AutoScroller = new AutoScroller(Rack.NetytarMainWindow., 0, 100, new PointFilterMAexpDecaying(0.07f)); // OLD was 100, 0.1f
            //Rack.NetytarDmiBox.NetytarSurface = new NetytarSurface(Rack.NetytarMainWindow.canvasNetytar, Rack.DrawMode);
            //Rack.NetytarDmiBox.NetytarSurface.DrawButtons();
            //Rack.NetytarDmiBox.NetytarSurface.Scale = new Scale(Rack.UserSettings.RootNote, Rack.UserSettings.ScaleCode);

            // Rendering module
            Rack.RenderingModule = new RenderingModule(Rack.InstrumentWindow);
            Rack.RenderingModule.StartRendering();

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
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables.Clear();
        }
    }
}