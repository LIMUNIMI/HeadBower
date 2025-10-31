using HeadBower.Modules;
using NITHlibrary.Nith.Internals;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Prevents note playing when mouth aperture is below threshold.
    /// Uses double threshold (hysteresis) to prevent flickering:
    /// - Gate closes (blocks notes) when mouth aperture falls below LOWER threshold (10)
    /// - Gate opens (allows notes) when mouth aperture rises above UPPER threshold (15)
    /// This behavior should run BEFORE BowMotionBehavior to set the gate state for the current frame.
    /// </summary>
    public class MouthClosedNotePreventionBehavior : INithSensorBehavior
    {
        // Double threshold constants for hysteresis
        private const double MOUTH_APERTURE_LOWER_THRESHOLD = 10.0;  // Gate closes below this
        private const double MOUTH_APERTURE_UPPER_THRESHOLD = 15.0;  // Gate opens above this

        // Required parameter: mouth_ape (double)
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.mouth_ape
        };

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Check if feature is enabled
                if (Rack.UserSettings.MouthClosedNotePreventionMode != _MouthClosedNotePreventionModes.On)
                {
                    // Feature disabled - open the gate (allow all note activation)
                    Rack.MappingModule.IsMouthGateBlocking = false;
                    return;
                }

                // ONLY process if mouth_ape parameter is present
                if (nithData.ContainsParameters(requiredParams))
                {
                    // Get mouth aperture value
                    var mouthApeValue = nithData.GetParameterValue(NithParameters.mouth_ape);
                    double mouthAperture = mouthApeValue.Value.ValueAsDouble;

                    // Apply hysteresis logic:
                    // If gate is currently BLOCKING (closed):
                    //   - Only open when aperture goes ABOVE upper threshold (15)
                    // If gate is currently OPEN:
                    //   - Only close when aperture falls below lower threshold (10)
                    
                    if (Rack.MappingModule.IsMouthGateBlocking)
                    {
                        // Gate is currently blocking - only open if aperture exceeds upper threshold
                        if (mouthAperture > MOUTH_APERTURE_UPPER_THRESHOLD)
                        {
                            Rack.MappingModule.IsMouthGateBlocking = false;
                        }
                        // else: stay blocked
                    }
                    else
                    {
                        // Gate is currently open - only close if aperture falls below lower threshold
                        if (mouthAperture < MOUTH_APERTURE_LOWER_THRESHOLD)
                        {
                            Rack.MappingModule.IsMouthGateBlocking = true;
                        }
                        // else: stay open
                    }

                    // NOTE: We do NOT force Blow = false here!
                    // The gate mechanism in MappingModule.Blow property will handle stopping
                    // This prevents flickering caused by fighting between behaviors
                }
                else
                {
                    // CRITICAL FIX: If mouth_ape is missing (e.g., webcam off, using phone head tracking),
                    // we MUST disable the gate to prevent blocking notes when feature can't function properly
                    // This ensures the behavior fails gracefully when mouth data is unavailable
                    Rack.MappingModule.IsMouthGateBlocking = false;
                }
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"MouthClosedNotePreventionBehavior Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                // On error, open the gate (fail-safe: allow notes)
                try { Rack.MappingModule.IsMouthGateBlocking = false; } catch { }
            }
        }
    }
}
