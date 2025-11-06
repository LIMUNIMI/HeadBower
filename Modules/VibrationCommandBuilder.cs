using System.Text;

namespace HeadBower.Modules
{
    /// <summary>
    /// Builds vibration commands in the NITH protocol format.
    /// Format: $issuer_name-version|COM|vibration_intensity=value&vibration_duration=value^
    /// </summary>
    public static class VibrationCommandBuilder
    {
        private const string ISSUER_NAME = "HeadBower";
        private const string VERSION = "0.1.0";
        private const string COMMAND_TYPE = "COM";
        private const char START_CHAR = '$';
        private const char END_CHAR = '^';

        /// <summary>
        /// Builds a vibration command string.
        /// </summary>
        /// <param name="intensity">Vibration intensity (0-255)</param>
        /// <param name="duration">Vibration duration (0-255)</param>
        /// <returns>Formatted command string</returns>
        public static string BuildCommand(int intensity, int duration)
        {
            // Clamp values to valid range
            intensity = Math.Clamp(intensity, 0, 255);
            duration = Math.Clamp(duration, 0, 255);

            // Build command: $HeadBower-0.1.0|COM|vibration_intensity=value&vibration_duration=value^
            StringBuilder sb = new StringBuilder();
            sb.Append(START_CHAR);
            sb.Append(ISSUER_NAME);
            sb.Append('-');
            sb.Append(VERSION);
            sb.Append('|');
            sb.Append(COMMAND_TYPE);
            sb.Append('|');
            sb.Append("vibration_intensity=");
            sb.Append(intensity);
            sb.Append('&');
            sb.Append("vibration_duration=");
            sb.Append(duration);
            sb.Append(END_CHAR);

            return sb.ToString();
        }
    }
}
