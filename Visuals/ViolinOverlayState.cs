namespace HeadBower.Visuals
{
    /// <summary>
    /// Holds visual feedback state for the violin overlay.
    /// This is populated by behaviors and consumed by ViolinOverlayManager for rendering.
    /// Completely decoupled from MappingModule - purely for visual feedback.
    /// All values are normalized for easy rendering.
    /// </summary>
    public class ViolinOverlayState
    {
        /// <summary>
        /// Normalized bow motion indicator (-1 to +1).
        /// Represents current head yaw velocity: negative = left, positive = right.
        /// Updated by VisualFeedbackBehavior.
        /// </summary>
        public double BowMotionIndicator { get; set; } = 0;

        /// <summary>
        /// Normalized head pitch position (-1 to +1) for visual feedback.
        /// Used to position the pitch indicator rectangle vertically.
        /// Updated by VisualFeedbackBehavior.
        /// </summary>
        public double PitchPosition { get; set; } = 0;

        /// <summary>
        /// Normalized pitch threshold value (0 to 1).
        /// Determines where the yellow threshold lines are drawn.
        /// Updated by VisualFeedbackBehavior from UserSettings.
        /// </summary>
        public double PitchThreshold { get; set; } = 0.3; // Default normalized threshold
    }
}
