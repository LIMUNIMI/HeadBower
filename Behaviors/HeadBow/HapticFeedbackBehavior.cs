using HeadBower.Modules;
using NITHlibrary.Nith.Internals;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Sends haptic vibration feedback to the phone based on bow pressure.
    /// Vibration intensity scales linearly from 0 (no pressure) to 200 (max pressure of 127).
    /// </summary>
    public class HapticFeedbackBehavior : INithSensorBehavior
    {
        // Vibration intensity constants
        private const int MAX_VIBRATION_INTENSITY = 200;
        private const int VIBRATION_INTERVAL_MS = 15;

        // State
        private DateTime _lastVibrationTime = DateTime.MinValue;

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Only send vibration when note is being played
                if (!Rack.MappingModule.Blow)
                {
                    return;
                }

                // Rate limit vibration updates
                if ((DateTime.Now - _lastVibrationTime).TotalMilliseconds < VIBRATION_INTERVAL_MS)
                {
                    return;
                }

                // Scale vibration linearly with pressure (0-127 ? 0-200)
                int pressure = Rack.MappingModule.Pressure;
                int vibIntensity = (int)((pressure / 127.0) * MAX_VIBRATION_INTENSITY);

                SendVibrationCommand(vibIntensity, vibIntensity);
                _lastVibrationTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"HapticFeedbackBehavior Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                // Silent failure for vibration - don't crash the pipeline
            }
        }

        /// <summary>
        /// Send a vibration command to the phone.
        /// </summary>
        private void SendVibrationCommand(int duration, int amplitude)
        {
            try
            {
                // Apply phone vibration sensitivity multiplier and clamp to 0-255
                duration = Math.Clamp((int)(duration * Rack.UserSettings.PhoneVibrationSensitivity), 0, 255);
                amplitude = Math.Clamp((int)(amplitude * Rack.UserSettings.PhoneVibrationSensitivity), 0, 255);
                
                // Build command using new NITH protocol format
                // Format: $HeadBower-0.1.0|COM|vibration_intensity=value&vibration_duration=value^
                string vibrationCommand = VibrationCommandBuilder.BuildCommand(amplitude, duration);
                
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
