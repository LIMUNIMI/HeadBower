using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Unified visual feedback behavior that updates the violin overlay state.
    /// Updates white ellipse (bow motion indicator) and red rectangle (pitch indicator).
    /// Runs every frame regardless of musical state - purely visual feedback.
    /// </summary>
    public class VisualFeedbackBehavior : INithSensorBehavior
    {
        // Constants
        private const double YAW_VELOCITY_SCALE = 10.0; // -10 to +10 m/s maps to -1 to +1

        // Filters for smooth visual feedback
        private readonly DoubleFilterMAexpDecaying _yawVelFilter = new(0.85f);
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new(0.9f);

        /// <summary>
        /// Handles incoming sensor data and updates visual feedback state.
        /// Always runs, regardless of whether a note is playing.
        /// </summary>
        public void HandleData(NithSensorData nithData)
        {
            UpdateBowMotionIndicator(nithData);
            UpdatePitchIndicator(nithData);
            UpdateThresholds();
        }

        /// <summary>
        /// Updates the white ellipse position based on head yaw velocity.
        /// </summary>
        private void UpdateBowMotionIndicator(NithSensorData nithData)
        {
            if (!nithData.ContainsParameter(NithParameters.head_vel_yaw))
                return;

            double rawVelocity = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;

            // Filter for smooth visuals
            _yawVelFilter.Push(rawVelocity);
            double filteredVelocity = _yawVelFilter.Pull();

            // Get sensitivity based on current head tracking source
            float sensitivity = GetCurrentSourceSensitivity();

            // Normalize to -1 to +1 range
            // Higher sensitivity = smaller head movement needed for same visual displacement
            double scaledVelocityScale = YAW_VELOCITY_SCALE / sensitivity;
            double normalizedPosition = Math.Max(-1.0, Math.Min(1.0, filteredVelocity / scaledVelocityScale));

            // Update visual state
            Rack.ViolinOverlayState.BowMotionIndicator = normalizedPosition;
        }

        /// <summary>
        /// Updates the red rectangle position based on head pitch position.
        /// </summary>
        private void UpdatePitchIndicator(NithSensorData nithData)
        {
            if (!nithData.ContainsParameter(NithParameters.head_pos_pitch))
                return;

            double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;

            // Filter for smooth visuals
            _pitchPosFilter.Push(rawPitch);
            double filteredPitch = _pitchPosFilter.Pull();

            // Normalize to -1 to +1 range based on pitch range setting
            double maxPitchRange = Rack.UserSettings.PitchRange;
            double normalizedPitch = Math.Max(-1.0, Math.Min(1.0, filteredPitch / maxPitchRange));

            // Update visual state (normalized value)
            Rack.ViolinOverlayState.PitchPosition = normalizedPitch;
        }

        /// <summary>
        /// Updates the yellow threshold lines position.
        /// </summary>
        private void UpdateThresholds()
        {
            // Get threshold from settings (shared across all behaviors)
            Rack.ViolinOverlayState.PitchThreshold = Rack.UserSettings.PitchThreshold / Rack.UserSettings.PitchRange;
        }

        /// <summary>
        /// Gets the sensitivity multiplier for the currently active head tracking source.
        /// </summary>
        private float GetCurrentSourceSensitivity()
        {
            return Rack.UserSettings.HeadTrackingSource switch
            {
                HeadTrackingSources.Webcam => Rack.UserSettings.WebcamSensitivity,
                HeadTrackingSources.Phone => Rack.UserSettings.PhoneSensitivity,
                HeadTrackingSources.EyeTracker => Rack.UserSettings.EyeTrackerSensitivity,
                _ => 1.0f
            };
        }
    }
}
