using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Sends haptic vibration feedback to the phone based on bow motion intensity.
    /// </summary>
    public class HapticFeedbackBehavior : INithSensorBehavior
    {
        // Required parameters (prefer acceleration, fallback to velocity)
        private readonly List<NithParameters> requiredParamsAcc = new List<NithParameters>
        {
            NithParameters.head_acc_yaw
        };

        private readonly List<NithParameters> requiredParamsVel = new List<NithParameters>
        {
            NithParameters.head_vel_yaw
        };

        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Constants
        private const double YAW_LOWERTHRESH_BASE = 1;
        private const int VIBRATION_INTERVAL_MS = 15;
        private const double VIBRATION_DIVIDER = 1.5;

        // Mapper
        private readonly SegmentMapper _vibrationMapper = new SegmentMapper(1, 50, 0, 250);

        // State
        private DateTime _lastVibrationTime = DateTime.MinValue;

        public void HandleData(NithSensorData nithData)
        {
            // Only send vibration when note is being played
            if (!Rack.MappingModule.Blow)
            {
                return;
            }

            // Try acceleration first, then velocity
            bool hasAcceleration = nithData.ContainsParameters(requiredParamsAcc);
            bool hasVelocity = nithData.ContainsParameters(requiredParamsVel);

            // ONLY process if at least one required parameter set is present
            if (hasAcceleration || hasVelocity)
            {
                // Get yaw motion magnitude
                double rawYawMotion;

                if (hasAcceleration)
                {
                    rawYawMotion = nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.ValueAsDouble;
                }
                else
                {
                    rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
                }

                // Apply sensitivity and get magnitude
                double yawMagnitude = Math.Abs(rawYawMotion * Sensitivity);

                // Check if we should send vibration
                if (yawMagnitude >= YAW_LOWERTHRESH_BASE &&
                    (DateTime.Now - _lastVibrationTime).TotalMilliseconds >= VIBRATION_INTERVAL_MS)
                {
                    int vibIntensity = (int)(_vibrationMapper.Map(yawMagnitude) / VIBRATION_DIVIDER);
                    SendVibrationCommand(vibIntensity, vibIntensity);
                    _lastVibrationTime = DateTime.Now;
                }
            }
            // If parameters missing, don't send vibration
        }

        /// <summary>
        /// Send a vibration command to the phone.
        /// </summary>
        private void SendVibrationCommand(int duration, int amplitude)
        {
            try
            {
                duration = Math.Clamp(duration, 0, 255);
                amplitude = Math.Clamp(amplitude, 0, 255);
                string vibrationCommand = $"VIB:{duration}:{amplitude}";
                if (Rack.NithSenderPhone != null)
                {
                    Rack.NithSenderPhone.SendData(vibrationCommand);
                }
            }
            catch
            {
                // Silent failure - no debug output
            }
        }
    }
}
