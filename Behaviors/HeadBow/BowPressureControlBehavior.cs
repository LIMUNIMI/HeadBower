using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Controls MIDI bow pressure (CC9) based on various input sources:
    /// - Head pitch rotation (INVERTED: 0 at upper threshold, 127 at lower threshold)
    /// - Mouth aperture (NORMAL: 0 at lower threshold, 127 at upper)
    /// Uses MappingModule.BowPressure property which respects On/Off toggle.
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
        private double _lastPitchRange = 0;

        public void HandleData(NithSensorData nithData)
        {
            // CRITICAL: Early exit if control is disabled or no note playing
            // This matches the pattern used in ModulationControlBehavior
            if (!Rack.MappingModule.Blow || 
                Rack.UserSettings.BowPressureControlMode != _BowPressureControlModes.On)
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

                try
                {
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

                                // INVERTED MAPPING for pitch
                                double pitchThreshold = Rack.UserSettings.PitchThreshold;
                                double maxPitchDeviation = Rack.UserSettings.PitchRange;
                                double absPitch = Math.Abs(filteredPitch);

                                if (absPitch <= pitchThreshold)
                                {
                                    bowPressureValue = 0;
                                }
                                else if (absPitch >= maxPitchDeviation)
                                {
                                    bowPressureValue = 127;
                                }
                                else
                                {
                                    // Recreate mapper only if thresholds changed
                                    // FIXED: Correct parameter order for inverted mapping
                                    // baseMin=threshold, baseMax=range, targetMin=0, targetMax=127
                                    // Then we invert the OUTPUT by doing (127 - mapped value)
                                    if (_pitchMapper == null || pitchThreshold != _lastPitchThreshold || maxPitchDeviation != _lastPitchRange)
                                    {
                                        _pitchMapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 0, 127, true);
                                        _lastPitchThreshold = pitchThreshold;
                                        _lastPitchRange = maxPitchDeviation;
                                    }
                                    // Map and then invert (127 - x gives inverted result)
                                    bowPressureValue = 127 - (int)_pitchMapper.Map(absPitch);
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
                }
                catch (Exception ex)
                {
                    // Log exception for debugging
                    Console.WriteLine($"BowPressureControlBehavior Exception: {ex.Message}");
                }


                // Set through MappingModule property
                Rack.MappingModule.BowPressure = bowPressureValue;
            }
            // If parameters missing, keep last bow pressure value (don't update)
        }
    }
}
