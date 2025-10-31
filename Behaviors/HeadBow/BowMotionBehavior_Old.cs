using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Mappers;
using NITHlibrary.Tools.Filters.ValueFilters;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// BACKUP OF ORIGINAL BEHAVIOR - Can be restored if needed
    /// Core bow motion detection using head yaw velocity OR mouth aperture.
    /// Handles note triggering, direction changes, and velocity mapping.
    /// Uses discrete direction indicators for instant bow direction changes without smoothing lag.
    /// Supports switching between head_vel_yaw and mouth_ape as pressure source.
    /// </summary>
    public class BowMotionBehavior_Old : INithSensorBehavior
    {
        // Required parameters for head yaw velocity mode
        private readonly List<NithParameters> requiredParamsYaw = new List<NithParameters>
        {
            NithParameters.head_vel_yaw
            // head_dir_yaw is optional (will fallback to Sign if not present)
        };

        // Required parameters for mouth aperture mode
        private readonly List<NithParameters> requiredParamsMouth = new List<NithParameters>
        {
            NithParameters.mouth_ape
        };

        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Bowing mode property
        public bool UseLogarithmicBowing { get; set; } = false;

        // Constants and thresholds for yaw velocity
        private const double YAW_UPPERTHRESH_BASE = 2;
        private const double YAW_LOWERTHRESH_BASE = 1;
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // Constants for mouth aperture
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double MOUTH_APERTURE_UPPER_THRESHOLD = 30.0;

        // Mappers
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(1, 10, 1, 127);
        private readonly SegmentMapper _mouthApertureMapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 1, 127, true);

        // Filter for mouth aperture
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // State for yaw velocity mode
        private int _currentDirection = 0;
        private int _previousDirection = 0;
        private double _yawMagnitude = 0;
        private DateTime _lastDirectionChangeTime = DateTime.MinValue;

        // State for mouth aperture mode
        private bool _wasBlowing = false;

        public void HandleData(NithSensorData nithData)
        {
            // Determine source based on settings
            PressureControlSources currentSource = Rack.UserSettings.PressureControlSource;

            switch (currentSource)
            {
                case PressureControlSources.HeadYawVelocity:
                    HandleYawVelocityMode(nithData);
                    break;

                case PressureControlSources.MouthAperture:
                    HandleMouthApertureMode(nithData);
                    break;
            }
        }

        private void HandleYawVelocityMode(NithSensorData nithData)
        {
            // ONLY process if ALL required parameters are present
            if (nithData.ContainsParameters(requiredParamsYaw))
            {
                // 1. Get yaw velocity for intensity and magnitude
                double rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;

                // Apply sensitivity multiplier
                rawYawMotion *= Sensitivity;

                // Update MappingModule with motion value
                Rack.MappingModule.HeadYawMotion = rawYawMotion;

                // 2. Get direction from discrete direction parameter (instant response, no smoothing lag)
                _previousDirection = _currentDirection;
                
                if (nithData.ContainsParameter(NithParameters.head_dir_yaw))
                {
                    _currentDirection = (int)nithData.GetParameterValue(NithParameters.head_dir_yaw).Value.ValueAsDouble;
                }
                else
                {
                    // Fallback to old method if direction parameter not available
                    _currentDirection = Math.Sign(rawYawMotion);
                }

                // Detect direction change (instant detection from discrete direction parameter)
                bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

                // 3. Calculate magnitude (using smoothed velocity for intensity)
                _yawMagnitude = Math.Abs(rawYawMotion);

                // 4. Map velocity to MIDI values
                double mappedYaw = 0;
                if (_yawMagnitude >= YAW_LOWERTHRESH_BASE)
                {
                    // Apply linear or logarithmic mapping based on setting
                    if (UseLogarithmicBowing)
                    {
                        mappedYaw = MapLogarithmicVelocity(_yawMagnitude);
                    }
                    else
                    {
                        mappedYaw = _yawVelMapper.Map(_yawMagnitude);
                    }
                }

                // Set pressure value in MappingModule (which updates MIDI CC9 and GUI progressbar)
                Rack.MappingModule.Pressure = (int)mappedYaw;

                // 5. Handle direction change (instant response - no lag!)
                if (isDirectionChanged)
                {
                    Rack.MappingModule.Blow = false;
                    _lastDirectionChangeTime = DateTime.Now;
                    Rack.MappingModule.IsPlayingViolin = false;
                }
                // 6. Handle pause after direction change
                else if ((DateTime.Now - _lastDirectionChangeTime).TotalMilliseconds < DIRECTION_CHANGE_PAUSE_MS)
                {
                    Rack.MappingModule.Blow = false;
                    Rack.MappingModule.IsPlayingViolin = false;
                }
                // 7. Normal note activation/deactivation
                else
                {
                    if (_yawMagnitude >= YAW_UPPERTHRESH_BASE && !Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, mappedYaw * 1.2));
                        Rack.MappingModule.Blow = true;
                        Rack.MappingModule.IsPlayingViolin = true;
                    }
                    else if (_yawMagnitude < YAW_LOWERTHRESH_BASE && Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Blow = false;
                        Rack.MappingModule.IsPlayingViolin = false;
                    }
                }
            }
            // If parameters missing, don't update anything (keep current state)
        }

        /// <summary>
        /// Maps yaw velocity to MIDI pressure using a logarithmic scale.
        /// This provides finer control at lower velocities and easier expressiveness.
        /// Logarithmic formula: output = base * log(1 + input/base) scaled to 1-127 range
        /// </summary>
        private double MapLogarithmicVelocity(double velocity)
        {
            // Logarithmic base for the mapping
            // Higher base = more compression in the logarithmic curve
            const double logBase = 2.0;
            
            // Apply logarithmic scaling
            // log(1 + x) ensures we don't get negative values with log at x=0
            double logScaled = Math.Log(1.0 + (velocity / logBase)) / Math.Log(logBase);
            
            // Map from logarithmic scale to MIDI range (1-127)
            // Input range: velocity from ~1 to 10 becomes log scale from ~0.3 to ~2.3
            const double minLog = 0.0;
            const double maxLog = 3.3; // log(1 + 10/2) / log(2) ≈ 2.3, add headroom
            
            double normalized = (logScaled - minLog) / (maxLog - minLog);
            double midiValue = 1 + (normalized * 126.0); // Map to 1-127
            
            return Math.Max(1, Math.Min(127, midiValue));
        }

        private void HandleMouthApertureMode(NithSensorData nithData)
        {
            // ONLY process if ALL required parameters are present
            if (nithData.ContainsParameters(requiredParamsMouth))
            {
                // 1. Get and filter mouth aperture
                double rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                _mouthApertureFilter.Push(rawMouthAperture);
                double filteredMouthAperture = _mouthApertureFilter.Pull();

                // 2. Map mouth aperture to pressure value
                double mappedPressure = 0;
                if (filteredMouthAperture >= MOUTH_APERTURE_THRESHOLD)
                {
                    mappedPressure = _mouthApertureMapper.Map(filteredMouthAperture);
                }

                // Set pressure value in MappingModule
                Rack.MappingModule.Pressure = (int)mappedPressure;

                // 3. Handle note activation/deactivation based on mouth aperture
                if (filteredMouthAperture >= MOUTH_APERTURE_UPPER_THRESHOLD && !Rack.MappingModule.Blow)
                {
                    // Start playing
                    Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, mappedPressure * 1.2));
                    Rack.MappingModule.Blow = true;
                    Rack.MappingModule.IsPlayingViolin = true;
                    _wasBlowing = true;
                }
                else if (filteredMouthAperture < MOUTH_APERTURE_THRESHOLD && Rack.MappingModule.Blow)
                {
                    // Stop playing
                    Rack.MappingModule.Blow = false;
                    Rack.MappingModule.IsPlayingViolin = false;
                    _wasBlowing = false;
                }
            }
            // If parameters missing, don't update anything (keep current state)
        }
    }
}
