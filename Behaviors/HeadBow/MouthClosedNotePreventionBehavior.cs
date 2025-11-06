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
                if (nithData.ContainsParameters(requiredParams))
                {
                    double mouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                    var blocking = Rack.MappingModule.IsMouthGateBlocking;
                    if (blocking && mouthAperture > MOUTH_APERTURE_UPPER_THRESHOLD)
                    {
                        Rack.MappingModule.IsMouthGateBlocking = false;
                    }
                    else if (!blocking && mouthAperture < MOUTH_APERTURE_LOWER_THRESHOLD)
                    {
                        Rack.MappingModule.IsMouthGateBlocking = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MouthClosedNotePreventionBehavior Exception: {ex.Message}");
            }
        }
    }
}
