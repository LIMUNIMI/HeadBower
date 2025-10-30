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

            // Get yaw motion magnitude
            double rawYawMotion = 0;

            if (nithData.ContainsParameter(NithParameters.head_acc_yaw))
            {
                rawYawMotion = nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.ValueAsDouble;
            }
            else if (nithData.ContainsParameter(NithParameters.head_vel_yaw))
            {
                rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
            }
            else
            {
                return;
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
