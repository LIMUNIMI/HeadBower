using System.Windows.Controls;
using HeadBower.Surface;
using NITHdmis.Music;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Nith.Wrappers;

namespace HeadBower.Modules
{
    /// <summary>
    /// DMIBox for Netytar, implementing the internal logic of the instrument.
    /// </summary>
    public class MappingModule
    {
        private const int MIN_VALUES_THRESHOLD = 1;
        private bool hasAButtonGaze = false;

        private Button lastGazedButton = new Button();

        private string testString;

        public MappingModule()
        {
            StartingScale = ScalesFactory.Cmaj;
            LastScale = StartingScale;
            SelectedScale = StartingScale;
        }

        // ============================================================================
        // HEAD MOTION PARAMETER DEFINITIONS
        // ============================================================================

        /// <summary>
        /// Parameters that represent head position data.
        /// These are the base position values from any source (webcam, phone, eye tracker).
        /// </summary>
        public static readonly List<NithParameters> HeadPositionParameters = new()
        {
            NithParameters.head_pos_yaw,
            NithParameters.head_pos_pitch,
            NithParameters.head_pos_roll
        };

        /// <summary>
        /// Parameters that represent head velocity data.
        /// These are calculated from position changes or sent directly by sensors.
        /// </summary>
        public static readonly List<NithParameters> HeadVelocityParameters = new()
        {
            NithParameters.head_vel_yaw,
            NithParameters.head_vel_pitch,
            NithParameters.head_vel_roll
        };

        /// <summary>
        /// Parameters that represent head acceleration data.
        /// These are calculated from velocity changes.
        /// </summary>
        public static readonly List<NithParameters> HeadAccelerationParameters = new()
        {
            NithParameters.head_acc_yaw,
            NithParameters.head_acc_pitch,
            NithParameters.head_acc_roll
        };

        /// <summary>
        /// All head motion parameters combined (position + velocity + acceleration).
        /// Used for whitelisting all motion data from the selected source.
        /// </summary>
        public static readonly List<NithParameters> AllHeadMotionParameters = new()
        {
            // Position
            NithParameters.head_pos_yaw,
            NithParameters.head_pos_pitch,
            NithParameters.head_pos_roll,
            // Velocity
            NithParameters.head_vel_yaw,
            NithParameters.head_vel_pitch,
            NithParameters.head_vel_roll,
            // Acceleration
            NithParameters.head_acc_yaw,
            NithParameters.head_acc_pitch,
            NithParameters.head_acc_roll
        };

        public bool CursorHidden { get; set; } = false;
        public bool HasAButtonGaze { get => hasAButtonGaze; set => hasAButtonGaze = value; }
        public Button LastGazedButton { get => lastGazedButton; set => lastGazedButton = value; }

        public string TestString { get => testString; set => testString = value; }
        private Scale LastScale { get; set; }
        private Scale SelectedScale { get; set; }
        private Scale StartingScale { get; set; }

        #region Mouth Gate Mechanism

        /// <summary>
        /// Indicates whether the mouth gate is currently blocking note activation.
        /// When true, the Blow property will refuse to activate (set to true).
        /// Set by MouthClosedNotePreventionBehavior based on mouth aperture (mouth_ape) with hysteresis:
        /// - Gate closes when mouth_ape falls below 10
        /// - Gate opens when mouth_ape rises above 15
        /// </summary>
        public bool IsMouthGateBlocking { get; set; } = false;

        #endregion Mouth Gate Mechanism

        #region Instrument logic

        public double BowPosition { get; set; } = 0; // Posizione dell'arco (da -1 a 1)
        public double PitchBendValue { get; set; } = 0; // Valore del pitch bend (da -1 a 1)
        public double HeadPitchPosition { get; set; } = 0; // Posizione raw del pitch
        public double PitchBendThreshold { get; set; } = 10.0; // Soglia per il pitch bend
        public bool IsPlayingViolin { get; set; } = false; // Se stiamo usando il violino
        public Button CurrentButton { get; set; } = null; // Pulsante attualmente selezionato
        public bool UsingAccelerationMode { get; set; } = false; // True when using acceleration (phone), false when using velocity
        public double HeadYawMotion { get; set; } = 0; // Stores the yaw motion value (acceleration or velocity) for display

        private bool blow = false;
        private int modulation = 0;
        private MidiNotes nextNote = MidiNotes.C5;
        private int pressure = 127;
        private int bowPressure = 0;
        private MidiNotes selectedNote = MidiNotes.C5;
        private int velocity = 127;

        public bool Blow
        {
            get { return blow; }
            set
            {
                // MOUTH GATE: Block activation if mouth is closed (when feature is enabled)
                if (value == true && IsMouthGateBlocking)
                {
                    // Mouth is closed - refuse activation
                    // Also ensure we stop if somehow we're still playing
                    if (blow == true)
                    {
                        blow = false;
                        StopSelectedNote();
                    }
                    return;
                }

                switch (Rack.UserSettings.SlidePlayMode)
                {
                    case _SlidePlayModes.On:
                        if (value != blow)
                        {
                            blow = value;
                            if (blow == true)
                            {
                                PlaySelectedNote();
                            }
                            else
                            {
                                StopSelectedNote();
                            }
                        }
                        break;

                    case _SlidePlayModes.Off:
                        if (value != blow)
                        {
                            blow = value;
                            if (blow == true)
                            {
                                selectedNote = nextNote;
                                PlaySelectedNote();
                            }
                            else
                            {
                                StopSelectedNote();
                            }
                        }
                        break;
                }
            }
        }

        public int Modulation
        {
            get { return modulation; }
            set
            {
                int newValue;
                
                if (Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On)
                {
                    // Clamp to MIDI range
                    newValue = Math.Clamp(value, 0, 127);
                }
                else
                {
                    newValue = 0;
                }
                
                // Only send MIDI if value actually changed
                if (newValue != modulation)
                {
                    modulation = newValue;
                    SetModulation();
                }
            }
        }

        public int Pressure
        {
            get { return pressure; }
            set
            {
                if (value < MIN_VALUES_THRESHOLD && value > 1)
                {
                    pressure = MIN_VALUES_THRESHOLD;
                }
                else if (value > 127)
                {
                    pressure = 127;
                }
                else if (value == 0)
                {
                    pressure = 0;
                }
                else
                {
                    pressure = value;
                }
                SetPressure();
            }
        }

        public int BowPressure
        {
            get { return bowPressure; }
            set
            {
                int newValue;
                
                if (Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On)
                {
                    // Clamp to MIDI range
                    newValue = Math.Clamp(value, 0, 127);
                }
                else
                {
                    // Force to zero when disabled
                    newValue = 0;
                }
                
                // Only send MIDI if value actually changed
                if (newValue != bowPressure)
                {
                    bowPressure = newValue;
                    SetBowPressure();
                }
            }
        }

        public MidiNotes SelectedNote
        {
            get { return selectedNote; }
            set
            {
                switch (Rack.UserSettings.SlidePlayMode)
                {
                    case _SlidePlayModes.On:
                        if (value != selectedNote)
                        {
                            StopSelectedNote();
                            selectedNote = value;
                            if (blow)
                            {
                                PlaySelectedNote();
                            }
                        }
                        break;

                    case _SlidePlayModes.Off:
                        if (value != selectedNote)
                        {
                            nextNote = value;
                        }
                        break;
                }
            }
        }

        public int Velocity
        {
            get { return velocity; }
            set
            {
                if (value < 0)
                {
                    velocity = 0;
                }
                else if (value > 127)
                {
                    velocity = 127;
                }
                else
                {
                    velocity = value;
                }
            }
        }

        public void ResetModulationAndPressure()
        {
            Blow = false;
            Modulation = 0;
            Pressure = 127;
            Velocity = 127;
        }

        private void PlaySelectedNote()
        {
            Rack.MidiModule.NoteOn((int)selectedNote, velocity);
        }

        private void SetModulation()
        {
            Rack.MidiModule.SetModulation(Modulation);
        }

        private void SetBowPressure()
        {
            Rack.MidiModule.SendControlChange(9, BowPressure);
        }

        private void SetPressure()
        {
            Rack.MidiModule.SetPressure(pressure);
        }

        private void StopSelectedNote()
        {
            Rack.MidiModule.NoteOff((int)selectedNote);
        }

        #endregion Instrument logic

        #region Head Tracking Source Selection

        /// <summary>
        /// Configures the parameter selector to whitelist head motion parameters from the selected source
        /// and block them from all other sources.
        /// ALWAYS whitelists: mouth_ape, eye apertures (from webcam), gaze parameters (from eye tracker).
        /// </summary>
        /// <param name="source">The head tracking source to enable</param>
        public static void SelectHeadTrackingSource(HeadTrackingSources source)
        {
            // Clear all existing rules
            Rack.ParameterSelector.ClearAllRules();

            // Get the sensor name for this source
            string selectedSensorName = GetSensorNameForSource(source);

            // Whitelist all head motion parameters for the selected source
            Rack.ParameterSelector.AddRulesList(selectedSensorName, AllHeadMotionParameters);

            // CRITICAL: Whitelist mouth parameters from WEBCAM ONLY
            // The webcam is the ONLY source that sends mouth_ape, eyeLeft_ape, eyeRight_ape
            // These must ALWAYS be whitelisted regardless of selected head tracking source
            // This ensures mouth gate, modulation control, and bow pressure control work with all head tracking sources
            
            Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.mouth_ape);
            Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeLeft_ape);
            Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeRight_ape);
            
            // ADDITIONAL FIX: Also whitelist mouth parameters from the SELECTED SOURCE
            // Some sensors (phone, eye tracker) may merge/forward webcam data with their own SensorName
            // This ensures mouth gate works even when data is merged
            if (selectedSensorName != "NITHwebcamWrapper")
            {
                Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.mouth_ape);
                Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeLeft_ape);
                Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeRight_ape);
            }
            
            // ALWAYS whitelist gaze from eye tracker (needed for gaze-to-mouse control)
            Rack.ParameterSelector.AddRule("NITHeyetrackerWrapper", NithParameters.gaze_x);
            Rack.ParameterSelector.AddRule("NITHeyetrackerWrapper", NithParameters.gaze_y);

            // Block head motion parameters from all OTHER sources
            BlockHeadMotionFromOtherSources(source);

            // Log configuration
            LogSelectionConfiguration(source);
        }

        /// <summary>
        /// Gets the sensor name that corresponds to a head tracking source.
        /// </summary>
        private static string GetSensorNameForSource(HeadTrackingSources source)
        {
            return source switch
            {
                HeadTrackingSources.Webcam => "NITHwebcamWrapper",
                HeadTrackingSources.Phone => "NITHphoneWrapper",
                HeadTrackingSources.EyeTracker => "NITHeyetrackerWrapper",
                _ => throw new ArgumentException($"Unknown head tracking source: {source}")
            };
        }

        /// <summary>
        /// Blocks head motion parameters from all sources except the selected one.
        /// This ensures only one source provides head motion data at a time.
        /// </summary>
        private static void BlockHeadMotionFromOtherSources(HeadTrackingSources selectedSource)
        {
            var allSources = new[]
            {
                (HeadTrackingSources.Webcam, "NITHwebcamWrapper"),
                (HeadTrackingSources.Phone, "NITHphoneWrapper"),
                (HeadTrackingSources.EyeTracker, "NITHeyetrackerWrapper")
            };

            foreach (var (source, sensorName) in allSources)
            {
                if (source != selectedSource)
                {
                    // In Whitelist mode, not adding rules = blocking all parameters
                    // So we don't need to do anything for non-selected sources
                    // They will be automatically blocked
                }
            }
        }

        /// <summary>
        /// Logs the current parameter selector configuration to console for debugging.
        /// </summary>
        private static void LogSelectionConfiguration(HeadTrackingSources source)
        {
            Console.WriteLine("\n=== HEAD TRACKING SOURCE SELECTION ===");
            Console.WriteLine($"Selected Source: {source}");
            Console.WriteLine($"Sensor Name Mapping:");
            Console.WriteLine($"  Webcam → NITHwebcamWrapper");
            Console.WriteLine($"  Phone → NITHphoneWrapper");
            Console.WriteLine($"  Eye Tracker → NITHeyetrackerWrapper");
            Console.WriteLine($"\nParameter Selector Rules:");
            Console.WriteLine(Rack.ParameterSelector.GetRulesSummary());
            Console.WriteLine("=====================================\n");
            
            // Log receiver connection status
            Console.WriteLine("=== RECEIVER CONNECTION STATUS ===");
            Console.WriteLine($"Webcam (port 20100): {(Rack.UDPreceiverWebcam?.IsConnected == true ? "CONNECTED" : "DISCONNECTED")}");
            Console.WriteLine($"Eye Tracker (port 20102): {(Rack.UDPreceiverEyeTracker?.IsConnected == true ? "CONNECTED" : "DISCONNECTED")}");
            Console.WriteLine($"Phone (port {(int)NithWrappersReceiverPorts.NITHphoneWrapper}): {(Rack.UDPreceiverPhone?.IsConnected == true ? "CONNECTED" : "DISCONNECTED")}");
            Console.WriteLine("=====================================\n");
        }

        #endregion Head Tracking Source Selection

        #region Shared values

        public Button CheckedButton { get; internal set; }
        [Obsolete("No longer used - prbIntensity progressbar now directly reflects Pressure (MIDI CC9) value")]
        public double IntensityIndicator { get; set; } = 0f;
        public double HeadYawPosition { get; internal set; }
        [Obsolete("No longer used - was replaced by direct Pressure value for progressbar feedback")]
        public int InputIndicatorValue { get; internal set; }

        #endregion Shared values

        // Aggiungi questo metodo nella regione #region Instrument logic
        public void SetPitchBend(double normalizedValue)
        {
            PitchBendValue = normalizedValue;
            // Conversione da -1..1 a 0..16383
            int pitchBendMidiValue = (int)(8192 + (normalizedValue * 8192));
            pitchBendMidiValue = Math.Clamp(pitchBendMidiValue, 0, 16383);
            Rack.MidiModule.SetPitchBend(pitchBendMidiValue);
        }
    }
}