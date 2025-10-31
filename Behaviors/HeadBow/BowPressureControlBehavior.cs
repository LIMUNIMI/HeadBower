using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Controls MIDI bow pressure (CC9) based on various input sources:
    /// - Head pitch rotation (INVERTED SIGNED: +threshold=0, 0=64, -threshold=127)
    /// - Mouth aperture (NORMAL: 0 at lower threshold, 127 at upper)
    /// Uses MappingModule.BowPressure property which respects On/Off toggle.
    /// CONTINUOUSLY sends bow pressure regardless of whether a note is playing.
    /// </summary>
    public class BowPressureControlBehavior : INithSensorBehavior
    {
        // Required parameters for pitch mode
        private readonly List<NithParameters> requiredParamsPitch = new List<NithParameters>
        {
            NithParameters.head_pos_pitch
        };

        // Required parameters for mouth mode
        private readonly List<NithParameters> requiredParamsMouth = new List<NithParameters>
        {
            NithParameters.mouth_ape
        };

        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        
        // Pre-create mappers to avoid allocations every frame
        private SegmentMapper _pitchMapper;
        private SegmentMapper _mouthMapper;
        private double _lastPitchThreshold = 0;

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Early exit only if control is disabled (removed Blow check)
                if (Rack.UserSettings.BowPressureControlMode != _BowPressureControlModes.On)
                {
                    Rack.MappingModule.BowPressure = 0;
                    return;
                }

                // Determine which source we're using and check if ALL required parameters are present
                BowPressureControlSources currentSource = Rack.UserSettings.BowPressureControlSource;
                
                // Select appropriate parameter list based on source
                List<NithParameters> requiredParams = currentSource == BowPressureControlSources.HeadPitch 
                    ? requiredParamsPitch 
                    : requiredParamsMouth;

                // ONLY process if ALL required parameters are present
                if (nithData.ContainsParameters(requiredParams))
                {
                    int bowPressureValue = 0;

                    switch (currentSource)
                    {
                        case BowPressureControlSources.HeadPitch:
                            // Get and filter pitch with null-safe access
                            var pitchParam = nithData.GetParameterValue(NithParameters.head_pos_pitch);
                            if (pitchParam.HasValue)
                            {
                                double rawPitch = pitchParam.Value.ValueAsDouble;
                                _pitchPosFilter.Push(rawPitch);
                                double filteredPitch = _pitchPosFilter.Pull();

                                // INVERTED SIGNED MAPPING for pitch
                                // Uses SIGNED pitch value (not absolute)
                                // +threshold ? 0 pressure
                                // 0 (neutral) ? ~64 pressure  
                                // -threshold ? 127 pressure
                                double pitchThreshold = Rack.UserSettings.PitchThreshold;

                                // Safety check for threshold
                                if (pitchThreshold <= 0)
                                {
                                    pitchThreshold = 15.0; // Default fallback
                                }

                                if (filteredPitch >= pitchThreshold)
                                {
                                    // Above upper threshold: clamp to zero
                                    bowPressureValue = 0;
                                }
                                else if (filteredPitch <= -pitchThreshold)
                                {
                                    // Below lower threshold: clamp to max
                                    bowPressureValue = 127;
                                }
                                else
                                {
                                    // Between thresholds: inverted linear mapping
                                    // Map from -threshold to +threshold ? 0 to 127, then invert
                                    // Recreate mapper only if threshold changed
                                    if (_pitchMapper == null || pitchThreshold != _lastPitchThreshold)
                                    {
                                        // Map from -threshold (min) to +threshold (max) ? 0 to 127
                                        _pitchMapper = new SegmentMapper(-pitchThreshold, pitchThreshold, 0, 127, true);
                                        _lastPitchThreshold = pitchThreshold;
                                    }
                                    // Map the value, then invert the result
                                    double mappedValue = _pitchMapper.Map(filteredPitch);
                                    bowPressureValue = 127 - (int)mappedValue;
                                }
                            }
                            break;

                        case BowPressureControlSources.MouthAperture:
                            // Get and filter mouth aperture with null-safe access
                            var mouthParam = nithData.GetParameterValue(NithParameters.mouth_ape);
                            if (mouthParam.HasValue)
                            {
                                double rawMouthAperture = mouthParam.Value.ValueAsDouble;
                                _mouthApertureFilter.Push(rawMouthAperture);
                                double filteredMouthAperture = _mouthApertureFilter.Pull();

                                // NORMAL MAPPING for mouth aperture
                                if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                                {
                                    bowPressureValue = 0;
                                }
                                else
                                {
                                    if (_mouthMapper == null)
                                    {
                                        _mouthMapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 0, 127, true);
                                    }
                                    bowPressureValue = (int)_mouthMapper.Map(filteredMouthAperture);
                                }
                            }
                            break;
                    }

                    // Set through MappingModule property - ALWAYS, not just when blowing
                    Rack.MappingModule.BowPressure = bowPressureValue;
                }
                // If parameters missing, keep last bow pressure value (don't update)
            }
            catch (Exception ex)
            {
                // Log exception for debugging with more detail
                Console.WriteLine($"BowPressureControlBehavior Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                // Set to zero on error
                try { Rack.MappingModule.BowPressure = 0; } catch { }
            }
        }
    }
}
