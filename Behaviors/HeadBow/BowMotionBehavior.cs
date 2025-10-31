using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Mappers;
using NITHlibrary.Tools.Filters.ValueFilters;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Core bow motion detection with separated gating and pressure control.
    /// HEAD YAW VELOCITY: Controls WHEN to play (gating, direction changes, deadzone)
    /// SELECTED SOURCE: Controls HOW LOUD to play (pressure value from Yaw or Mouth)
    /// This allows mouth-controlled pressure while still using head motion for note triggering.
    /// Uses hysteresis for stable gating without flickering.
    /// </summary>
    public class BowMotionBehavior : INithSensorBehavior
    {
        // Required parameters - we need both yaw for gating AND selected source for pressure
        private readonly List<NithParameters> requiredParamsYaw = new List<NithParameters>
        {
            NithParameters.head_vel_yaw
            // head_dir_yaw is optional (will fallback to Sign if not present)
        };

        private readonly List<NithParameters> requiredParamsMouth = new List<NithParameters>
        {
            NithParameters.mouth_ape
        };

        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Bowing mode property
        public bool UseLogarithmicBowing { get; set; } = false;

        // Constants and thresholds for yaw velocity (GATING with HYSTERESIS)
        private const double YAW_UPPERTHRESH_BASE = 2;   // Gate opens when magnitude exceeds this
        private const double YAW_LOWERTHRESH_BASE = 1;   // Gate closes when magnitude falls below this
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // Constants for mouth aperture (PRESSURE)
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;

        // Mappers
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(1, 10, 1, 127);
        private readonly SegmentMapper _mouthApertureMapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 1, 127, true);

        // Filter for mouth aperture
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // State for yaw velocity (GATING - always tracked)
        private int _currentDirection = 0;
        private int _previousDirection = 0;
        private double _yawMagnitude = 0;
        private DateTime _lastDirectionChangeTime = DateTime.MinValue;
        
        // State for hysteresis-based gate control
        private bool _isYawGateOpen = false;  // Track gate state for hysteresis

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // CRITICAL: Different packets contain different parameters
                // - Yaw gating needs head_vel_yaw (from selected head tracking source)
                // - Pressure source might need mouth_ape (from webcam only)
                // We process whatever we can from THIS packet
                
                // Check if this packet has yaw data (for gating)
                bool hasYawData = nithData.ContainsParameters(requiredParamsYaw);
                
                if (hasYawData)
                {
                    // STEP 1: Process yaw for gating (direction changes, deadzone)
                    bool yawGateOpen = ProcessYawGating(nithData);

                    // STEP 2: Get pressure value from selected source (if available in this packet)
                    double pressureValue = GetPressureFromSource(nithData);

                    // STEP 3: Set pressure in MappingModule (updates MIDI CC9 and GUI)
                    // Only update if we got a valid pressure value
                    if (pressureValue > 0)
                    {
                        Rack.MappingModule.Pressure = (int)pressureValue;
                    }

                    // STEP 4: Handle note activation/deactivation based on yaw gating
                    if (yawGateOpen && !Rack.MappingModule.Blow)
                    {
                        // Start playing - use current pressure value for velocity
                        int currentPressure = Rack.MappingModule.Pressure;
                        Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, currentPressure * 1.2));
                        Rack.MappingModule.Blow = true;
                        Rack.MappingModule.IsPlayingViolin = true;
                    }
                    else if (!yawGateOpen && Rack.MappingModule.Blow)
                    {
                        // Stop playing
                        Rack.MappingModule.Blow = false;
                        Rack.MappingModule.IsPlayingViolin = false;
                    }
                }
                else
                {
                    // This packet doesn't have yaw data (might be webcam packet with mouth data)
                    // Try to update pressure if we're using mouth source
                    PressureControlSources currentSource = Rack.UserSettings.PressureControlSource;
                    if (currentSource == PressureControlSources.MouthAperture)
                    {
                        double pressureValue = GetPressureFromSource(nithData);
                        if (pressureValue > 0)
                        {
                            Rack.MappingModule.Pressure = (int)pressureValue;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                // Log error but do not crash
                Console.WriteLine($"[BowMotionBehavior] Error processing sensor data: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes head yaw for gating control (WHEN to play).
        /// Returns true if the gate should be open (allowing notes to play).
        /// Handles direction changes and deadzone WITH HYSTERESIS to prevent flickering.
        /// </summary>
        private bool ProcessYawGating(NithSensorData nithData)
        {
            // Check if yaw parameters are available
            if (!nithData.ContainsParameters(requiredParamsYaw))
            {
                // If yaw not available, keep current gate state (don't change anything)
                return _isYawGateOpen;
            }

            // 1. Get yaw velocity for gating
            double rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;

            // Apply sensitivity multiplier
            rawYawMotion *= Sensitivity;

            // Update MappingModule with motion value
            Rack.MappingModule.HeadYawMotion = rawYawMotion;

            // 2. Get direction from discrete direction parameter
            _previousDirection = _currentDirection;
            
            if (nithData.ContainsParameter(NithParameters.head_dir_yaw))
            {
                _currentDirection = (int)nithData.GetParameterValue(NithParameters.head_dir_yaw).Value.ValueAsDouble;
            }
            else
            {
                // Fallback to sign if direction parameter not available
                _currentDirection = Math.Sign(rawYawMotion);
            }

            // Detect direction change (triggers note repetition)
            bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

            // 3. Calculate magnitude for deadzone check
            _yawMagnitude = Math.Abs(rawYawMotion);

            // 4. Determine gate state WITH HYSTERESIS
            
            // Direction change: close gate briefly to retrigger note
            if (isDirectionChanged)
            {
                _lastDirectionChangeTime = DateTime.Now;
                _isYawGateOpen = false;
                return false; // Gate CLOSED
            }
            
            // Pause after direction change
            if ((DateTime.Now - _lastDirectionChangeTime).TotalMilliseconds < DIRECTION_CHANGE_PAUSE_MS)
            {
                _isYawGateOpen = false;
                return false; // Gate CLOSED
            }
            
            // Normal deadzone logic with HYSTERESIS
            // This prevents flickering when magnitude hovers near threshold
            if (_isYawGateOpen)
            {
                // Gate is currently OPEN - only close if magnitude falls below LOWER threshold
                if (_yawMagnitude < YAW_LOWERTHRESH_BASE)
                {
                    _isYawGateOpen = false;
                    return false; // Gate CLOSED
                }
                else
                {
                    return true; // Gate stays OPEN (hysteresis zone)
                }
            }
            else
            {
                // Gate is currently CLOSED - only open if magnitude exceeds UPPER threshold
                if (_yawMagnitude >= YAW_UPPERTHRESH_BASE)
                {
                    _isYawGateOpen = true;
                    return true; // Gate OPEN
                }
                else
                {
                    return false; // Gate stays CLOSED (hysteresis zone)
                }
            }
        }

        /// <summary>
        /// Gets the pressure value from the selected source (HOW LOUD to play).
        /// Returns MIDI pressure value (0-127).
        /// </summary>
        private double GetPressureFromSource(NithSensorData nithData)
        {
            PressureControlSources currentSource = Rack.UserSettings.PressureControlSource;

            switch (currentSource)
            {
                case PressureControlSources.HeadYawVelocity:
                    return GetPressureFromYaw(nithData);

                case PressureControlSources.MouthAperture:
                    return GetPressureFromMouth(nithData);

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Calculates pressure from head yaw velocity.
        /// </summary>
        private double GetPressureFromYaw(NithSensorData nithData)
        {
            if (!nithData.ContainsParameters(requiredParamsYaw))
            {
                return 0;
            }

            // Get yaw magnitude (already calculated in ProcessYawGating)
            // But we need to recalculate here in case ProcessYawGating wasn't called
            double rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
            rawYawMotion *= Sensitivity;
            double magnitude = Math.Abs(rawYawMotion);

            // Map velocity to MIDI pressure
            if (magnitude >= YAW_LOWERTHRESH_BASE)
            {
                if (UseLogarithmicBowing)
                {
                    return MapLogarithmicVelocity(magnitude);
                }
                else
                {
                    return _yawVelMapper.Map(magnitude);
                }
            }

            return 0;
        }

        /// <summary>
        /// Calculates pressure from mouth aperture.
        /// </summary>
        private double GetPressureFromMouth(NithSensorData nithData)
        {
            if (!nithData.ContainsParameters(requiredParamsMouth))
            {
                return 0;
            }

            // Get and filter mouth aperture
            double rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
            _mouthApertureFilter.Push(rawMouthAperture);
            double filteredMouthAperture = _mouthApertureFilter.Pull();

            // Map mouth aperture to pressure
            if (filteredMouthAperture >= MOUTH_APERTURE_THRESHOLD)
            {
                return _mouthApertureMapper.Map(filteredMouthAperture);
            }

            return 0;
        }

        /// <summary>
        /// Maps yaw velocity to MIDI pressure using a logarithmic scale.
        /// This provides finer control at lower velocities and easier expressiveness.
        /// </summary>
        private double MapLogarithmicVelocity(double velocity)
        {
            const double logBase = 2.0;
            double logScaled = Math.Log(1.0 + (velocity / logBase)) / Math.Log(logBase);
            
            const double minLog = 0.0;
            const double maxLog = 3.3;
            
            double normalized = (logScaled - minLog) / (maxLog - minLog);
            double midiValue = 1 + (normalized * 126.0);
            
            return Math.Max(1, Math.Min(127, midiValue));
        }
    }
}
