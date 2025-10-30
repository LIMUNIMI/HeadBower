using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using System;
using System.Collections.Generic;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Pure visual feedback behavior that updates the bow motion indicator ellipse.
    /// Reads head_vel_yaw and maps it to a normalized position (-1 to +1).
    /// Does NOT trigger notes or affect musical state - only visual feedback.
    /// </summary>
    public class BowMotionIndicatorBehavior : INithSensorBehavior
    {
        // Sensitivity property (shares the same slider as BowMotionBehavior)
        public float Sensitivity { get; set; } = 1.0f;

        // Constants
        private const double VELOCITY_SCALE = 10.0; // -10 to +10 m/s maps to -1 to +1

        // Filter for smooth visual feedback
        private readonly DoubleFilterMAexpDecaying _velocityFilter = new DoubleFilterMAexpDecaying(0.85f);

        public void HandleData(NithSensorData nithData)
        {
            // Read head yaw velocity
            var velParam = nithData.GetParameterValue(NithParameters.head_vel_yaw);
            if (velParam == null) return;
            
            double rawVelocity = velParam.Value.ValueAsDouble;

            // Filter for smooth visuals (before applying sensitivity)
            _velocityFilter.Push(rawVelocity);
            double filteredVelocity = _velocityFilter.Pull();

            // Normalize to -1 to +1 range
            // Sensitivity scales the "responsiveness" - higher sensitivity = smaller head movement needed
            double scaledVelocityScale = VELOCITY_SCALE / Sensitivity;
            double normalizedPosition = Math.Max(-1.0, Math.Min(1.0, filteredVelocity / scaledVelocityScale));

            // Update visual state
            Rack.ViolinOverlayState.BowMotionIndicator = normalizedPosition;
        }
    }
}
