using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Core bow motion detection using head yaw velocity.
    /// Handles note triggering, direction changes, and velocity mapping.
    /// </summary>
    public class BowMotionBehavior : INithSensorBehavior
    {
        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Required parameters
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            // We need head_vel_yaw for bow motion
        };

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
            // 1. Get yaw velocity (only velocity, not acceleration)
            double rawYawMotion = 0;

            if (nithData.ContainsParameter(NithParameters.head_vel_yaw))
            {
                rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
            }
            else
            {
                // No velocity data available
                return;
            }

            // Apply sensitivity multiplier
            rawYawMotion *= Sensitivity;

            // Update MappingModule with motion value
            Rack.MappingModule.HeadYawMotion = rawYawMotion;

            // 2. Determine direction and update state
            _previousDirection = _currentDirection;
            _currentDirection = Math.Sign(rawYawMotion);
            bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

            // 3. Calculate magnitude
            _yawMagnitude = Math.Abs(rawYawMotion);

            // 4. Map velocity to MIDI values
            double mappedYaw = 0;
            if (_yawMagnitude >= YAW_LOWERTHRESH_BASE)
            {
                mappedYaw = _yawVelMapper.Map(_yawMagnitude);
            }

            // Set values in MappingModule
            Rack.MappingModule.Pressure = (int)mappedYaw;
            Rack.MappingModule.InputIndicatorValue = (int)mappedYaw;

            // 5. Handle direction change
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
    }
}
