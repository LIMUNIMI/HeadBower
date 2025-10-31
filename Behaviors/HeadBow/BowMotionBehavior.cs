using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Core bow motion detection using head yaw velocity and direction.
    /// Handles note triggering, direction changes, and velocity mapping.
    /// Uses discrete direction indicators for instant bow direction changes without smoothing lag.
    /// </summary>
    public class BowMotionBehavior : INithSensorBehavior
    {
        // Required parameters
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_vel_yaw
            // head_dir_yaw is optional (will fallback to Sign if not present)
        };

        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Constants and thresholds
        private const double YAW_UPPERTHRESH_BASE = 2;
        private const double YAW_LOWERTHRESH_BASE = 1;
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // Mappers
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(1, 10, 1, 127);

        // State
        private int _currentDirection = 0;
        private int _previousDirection = 0;
        private double _yawMagnitude = 0;
        private DateTime _lastDirectionChangeTime = DateTime.MinValue;

        public void HandleData(NithSensorData nithData)
        {
            // ONLY process if ALL required parameters are present
            if (nithData.ContainsParameters(requiredParams))
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
                    mappedYaw = _yawVelMapper.Map(_yawMagnitude);
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
    }
}
