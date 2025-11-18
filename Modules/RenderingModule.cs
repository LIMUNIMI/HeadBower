using HeadBower;
using HeadBower.Visuals;
using NITHdmis.Music;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HeadBower.Modules
{
    /// <summary>
    /// The RenderingModule class is responsible for ALL rendering and UI updates in the application.
    /// It runs a unified update loop at 60fps that polls state and updates all UI elements.
    /// </summary>
    public class RenderingModule : IDisposable
    {
        private readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Colors.LightGreen);
        private readonly SolidColorBrush BlankBrush = new SolidColorBrush(Colors.Black);
        private readonly SolidColorBrush WarningBrush = new SolidColorBrush(Colors.DarkRed);

        private DispatcherTimer DispatcherTimer { get; set; }
        private MainWindow InstrumentWindow { get; set; }
        private ViolinOverlayManager overlayManager; // Owns the violin overlay visualization

        /// <summary>
        /// Initializes a new instance of the RenderingModule class.
        /// </summary>
        /// <param name="instrumentWindow">The main window instance where rendering will take place.</param>
        /// <param name="violinOverlayCanvas">The canvas to use for violin overlay (ViolinOverlayViolin or ViolinOverlayCircle)</param>
        public RenderingModule(MainWindow instrumentWindow, Canvas violinOverlayCanvas)
        {
            InstrumentWindow = instrumentWindow;
            overlayManager = new ViolinOverlayManager(violinOverlayCanvas);
            
            DispatcherTimer = new DispatcherTimer();
            DispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 16); // ~60fps
            DispatcherTimer.Tick += DispatcherUpdate;
        }

        /// <summary>
        /// Switches the violin overlay to a different canvas (e.g., when switching between Circle/Violin layouts)
        /// </summary>
        public void SetViolinOverlayCanvas(Canvas newCanvas)
        {
            overlayManager = new ViolinOverlayManager(newCanvas);
        }

        /// <summary>
        /// Releases the resources used by the RenderingModule.
        /// This stops the rendering timer.
        /// </summary>
        public void Dispose()
        {
            DispatcherTimer.Stop();
        }

        /// <summary>
        /// Starts the rendering timer which will invoke the update method at regular intervals.
        /// </summary>
        public void StartRendering()
        {
            DispatcherTimer.Start();
        }

        /// <summary>
        /// Stops the rendering timer to prevent further updates.
        /// </summary>
        public void StopRendering()
        {
            DispatcherTimer.Stop();
        }

        /// <summary>
        /// UNIFIED UPDATE LOOP - Called at 60fps to update ALL UI elements.
        /// Replaces: UpdateTimedVisuals, UpdateGUIVisuals, CheckMidiPort, Update_SensorIntensityVisuals, PositionViolinOverlay, ReceiveBlowingChange, ReceiveNoteChange.
        /// </summary>
        private void DispatcherUpdate(object sender, EventArgs e)
        {
            if (InstrumentWindow == null)
            {
                return;
            }

            try
            {
                // ============================================================
                // TIMED VISUALS (updated every frame)
                // ============================================================
                
                // Note information
                InstrumentWindow.txtNoteName.Text = Rack.MappingModule.SelectedNote.ToStandardString();
                InstrumentWindow.txtPitch.Text = Rack.MappingModule.SelectedNote.ToPitchValue().ToString();
                
                // Blowing status
                InstrumentWindow.txtIsBlowing.Text = Rack.MappingModule.Blow ? "B" : "_";
                
                // Pressure progressbar (MIDI CC9 value: 0-127)
                InstrumentWindow.prbIntensity.Value = Rack.MappingModule.Pressure;

                // Modulation progressbar (MIDI CC1 value: 0-127) - for debugging
                InstrumentWindow.prbModulation.Value = Rack.MappingModule.Modulation;

                // Violin overlay positioning
                if (Rack.MappingModule.CurrentButton != null)
                {
                    overlayManager?.UpdateOverlay(
                        Rack.MappingModule.CurrentButton, 
                        Rack.ViolinOverlayState.BowMotionIndicator, 
                        Rack.ViolinOverlayState.PitchPosition, 
                        Rack.ViolinOverlayState.PitchThreshold
                    );
                }
                else if (overlayManager != null)
                {
                    overlayManager.overlayCanvas.Visibility = Visibility.Collapsed;
                }

                // ============================================================
                // GUI VISUALS (settings indicators, text fields)
                // ============================================================
                
                // TEXT FIELDS
                InstrumentWindow.txtMIDIch.Text = "MP" + Rack.MidiModule.OutDevice.ToString();
                InstrumentWindow.txtSensorPort1.Text = "COM" + Rack.UserSettings.SensorPort1.ToString();
                InstrumentWindow.txtSensorPort2.Text = "COM" + Rack.UserSettings.SensorPort2.ToString();

                // INTERACTION MODE INDICATORS
                InstrumentWindow.indHeadBow.Background = Rack.UserSettings.InteractionMapping == InteractionMappings.HeadBow ? ActiveBrush : BlankBrush;
                
                // HEAD TRACKING SOURCE INDICATORS
                InstrumentWindow.indEyeTracker.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.EyeTracker ? ActiveBrush : BlankBrush;
                InstrumentWindow.indWebcam.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.Webcam ? ActiveBrush : BlankBrush;
                InstrumentWindow.indPhone.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.Phone ? ActiveBrush : BlankBrush;
                InstrumentWindow.indNITHheadTracker.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.NITHheadTracker ? ActiveBrush : BlankBrush;
                
                // FEATURE TOGGLE INDICATORS
                InstrumentWindow.indMod.Background = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? ActiveBrush : BlankBrush;
                InstrumentWindow.indBowPress.Background = Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On ? ActiveBrush : BlankBrush;
                InstrumentWindow.indSlidePlay.Background = Rack.UserSettings.SlidePlayMode == _SlidePlayModes.On ? ActiveBrush : BlankBrush;
                InstrumentWindow.indLogarithmicBowing.Background = Rack.UserSettings.UseLogarithmicBowing ? ActiveBrush : BlankBrush;
                InstrumentWindow.indMouthClosedPrevention.Background = Rack.UserSettings.MouthClosedNotePreventionMode == _MouthClosedNotePreventionModes.On ? ActiveBrush : BlankBrush;
                InstrumentWindow.indToggleCursor.Background = Rack.MappingModule.CursorHidden ? ActiveBrush : BlankBrush;
                InstrumentWindow.indToggleAutoScroll.Background = Rack.AutoScroller.Enabled ? ActiveBrush : BlankBrush;
                InstrumentWindow.indToggleEyeTracker.Background = Rack.GazeToMouse.Enabled ? ActiveBrush : BlankBrush;
                InstrumentWindow.indSettings.Background = InstrumentWindow.IsSettingsShown ? ActiveBrush : BlankBrush;
                
                // MODULATION SOURCE INDICATORS
                InstrumentWindow.indModSourcePitch.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.HeadPitch ? ActiveBrush : BlankBrush;
                InstrumentWindow.indModSourceMouth.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.MouthAperture ? ActiveBrush : BlankBrush;
                InstrumentWindow.indModSourceBreath.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.BreathPressure ? ActiveBrush : BlankBrush;
                InstrumentWindow.indModSourceTeeth.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.TeethPressure ? ActiveBrush : BlankBrush;
                
                // BOW PRESSURE SOURCE INDICATORS
                InstrumentWindow.indBowPressureSourcePitch.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.HeadPitch ? ActiveBrush : BlankBrush;
                InstrumentWindow.indBowPressureSourceMouth.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.MouthAperture ? ActiveBrush : BlankBrush;
                InstrumentWindow.indBowPressureSourceBreath.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.BreathPressure ? ActiveBrush : BlankBrush;
                InstrumentWindow.indBowPressureSourceTeeth.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.TeethPressure ? ActiveBrush : BlankBrush;

                // PRESSURE (INTENSITY) SOURCE INDICATORS
                InstrumentWindow.indPressureSourceYaw.Background = Rack.UserSettings.PressureControlSource == PressureControlSources.HeadYawVelocity ? ActiveBrush : BlankBrush;
                InstrumentWindow.indPressureSourceMouth.Background = Rack.UserSettings.PressureControlSource == PressureControlSources.MouthAperture ? ActiveBrush : BlankBrush;

                // ============================================================
                // MIDI PORT STATUS CHECK
                // ============================================================
                if (Rack.MidiModule.IsMidiOk())
                {
                    InstrumentWindow.txtMIDIch.Foreground = ActiveBrush;
                }
                else
                {
                    InstrumentWindow.txtMIDIch.Foreground = WarningBrush;
                }

                // ============================================================
                // SENSOR INTENSITY VISUALS
                // ============================================================
                switch (Rack.UserSettings.InteractionMapping)
                {
                    case InteractionMappings.HeadBow:
                        InstrumentWindow.txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityHead.ToString("F1");
                        
                        // Display pitch sensitivity for current head tracking source
                        float currentPitchSensitivity = Rack.UserSettings.HeadTrackingSource switch
                        {
                            HeadTrackingSources.Webcam => Rack.UserSettings.WebcamPitchSensitivity,
                            HeadTrackingSources.Phone => Rack.UserSettings.PhonePitchSensitivity,
                            HeadTrackingSources.EyeTracker => Rack.UserSettings.EyeTrackerPitchSensitivity,
                            HeadTrackingSources.NITHheadTracker => 1.0f, // NITHheadTracker uses default sensitivity
                            _ => 1.0f
                        };
                        InstrumentWindow.txtPitchSensitivity.Text = currentPitchSensitivity.ToString("F1");
                        
                        // Display yaw filter alpha
                        InstrumentWindow.txtYawFilterAlpha.Text = Rack.UserSettings.YawFilterAlpha.ToString("F2");
                        
                        // Display phone vibration sensitivity
                        InstrumentWindow.txtPhoneVibrationSensitivity.Text = Rack.UserSettings.PhoneVibrationSensitivity.ToString("F1");
                        break;
                    default:
                        break;
                }
            }
            catch
            {
                // Ignore errors to prevent rendering loop crashes
            }
        }

        /// <summary>
        /// Notify the rendering module that the phone IP changed and update the UI accordingly.
        /// This is called from background threads (discovery service), so we ensure UI thread access.
        /// </summary>
        /// <param name="ip">The new phone IP address.</param>
        public void NotifyPhoneIpChanged(string ip)
        {
            if (InstrumentWindow == null)
            {
                return;
            }

            // Ensure UI update runs on UI thread
            InstrumentWindow.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // Directly update the IP address textbox
                    if (InstrumentWindow.txtIPAddress != null)
                    {
                        InstrumentWindow.txtIPAddress.Text = ip;
                    }
                }
                catch
                {
                    // Silent failure to avoid crashing rendering thread
                }
            });
        }
    }
}