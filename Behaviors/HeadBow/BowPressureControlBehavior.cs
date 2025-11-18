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
    /// - Breath pressure (NORMAL: 0-100 normalized, mapped to 0-127 with deadzones at 10 and 90)
    /// - Teeth pressure (NORMAL: 0-100 normalized, mapped to 0-127 with deadzones at 10 and 90)
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

        // Required parameters for breath pressure mode
        private readonly List<NithParameters> requiredParamsBreath = new List<NithParameters>
        {
            NithParameters.breath_press
        };

        // Required parameters for teeth pressure mode
        private readonly List<NithParameters> requiredParamsTeeth = new List<NithParameters>
        {
            NithParameters.teeth_press
        };

        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);
        private readonly DoubleFilterMAexpDecaying _breathPressureFilter = new DoubleFilterMAexpDecaying(0.7f);
        private readonly DoubleFilterMAexpDecaying _teethPressureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double BREATH_DEADZONE_LOW = 10.0;
        private const double BREATH_DEADZONE_HIGH = 90.0;
        private const double TEETH_DEADZONE_LOW = 10.0;
        private const double TEETH_DEADZONE_HIGH = 90.0;
        
        // Pre-create mappers to avoid allocations every frame
        private SegmentMapper _pitchMapper;
        private SegmentMapper _mouthMapper;
        private SegmentMapper _breathMapper;
        private SegmentMapper _teethMapper;
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
                List<NithParameters> requiredParams = currentSource switch
                {
                    BowPressureControlSources.HeadPitch => requiredParamsPitch,
                    BowPressureControlSources.MouthAperture => requiredParamsMouth,
                    BowPressureControlSources.BreathPressure => requiredParamsBreath,
                    BowPressureControlSources.TeethPressure => requiredParamsTeeth,
                    _ => requiredParamsPitch
                };

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

                                double pitchThreshold = Rack.UserSettings.PitchThreshold;

                                if (pitchThreshold <= 0)
                                {
                                    pitchThreshold = 15.0;
                                }

                                if (filteredPitch >= pitchThreshold)
                                {
                                    bowPressureValue = 0;
                                }
                                else if (filteredPitch <= -pitchThreshold)
                                {
                                    bowPressureValue = 127;
                                }
                                else
                                {
                                    if (_pitchMapper == null || pitchThreshold != _lastPitchThreshold)
                                    {
                                        _pitchMapper = new SegmentMapper(-pitchThreshold, pitchThreshold, 0, 127, true);
                                        _lastPitchThreshold = pitchThreshold;
                                    }
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

                        case BowPressureControlSources.BreathPressure:
                            // Get normalized breath pressure (0-100 range)
                            var breathParam = nithData.GetParameterValue(NithParameters.breath_press);
                            if (breathParam.HasValue)
                            {
                                double rawBreathPressure = breathParam.Value.ValueAsDouble;
                                _breathPressureFilter.Push(rawBreathPressure);
                                double filteredBreathPressure = _breathPressureFilter.Pull();

                                // Apply deadzones at 10 and 90
                                if (filteredBreathPressure <= BREATH_DEADZONE_LOW)
                                {
                                    bowPressureValue = 0;
                                }
                                else if (filteredBreathPressure >= BREATH_DEADZONE_HIGH)
                                {
                                    bowPressureValue = 127;
                                }
                                else
                                {
                                    // Map from 10-90 range to 0-127
                                    if (_breathMapper == null)
                                    {
                                        _breathMapper = new SegmentMapper(BREATH_DEADZONE_LOW, BREATH_DEADZONE_HIGH, 0, 127, true);
                                    }
                                    bowPressureValue = (int)_breathMapper.Map(filteredBreathPressure);
                                }
                            }
                            break;

                        case BowPressureControlSources.TeethPressure:
                            // Get normalized teeth pressure (0-100 range)
                            var teethParam = nithData.GetParameterValue(NithParameters.teeth_press);
                            if (teethParam.HasValue)
                            {
                                double rawTeethPressure = teethParam.Value.ValueAsDouble;
                                _teethPressureFilter.Push(rawTeethPressure);
                                double filteredTeethPressure = _teethPressureFilter.Pull();

                                // Apply deadzones at 10 and 90
                                if (filteredTeethPressure <= TEETH_DEADZONE_LOW)
                                {
                                    bowPressureValue = 0;
                                }
                                else if (filteredTeethPressure >= TEETH_DEADZONE_HIGH)
                                {
                                    bowPressureValue = 127;
                                }
                                else
                                {
                                    // Map from 10-90 range to 0-127
                                    if (_teethMapper == null)
                                    {
                                        _teethMapper = new SegmentMapper(TEETH_DEADZONE_LOW, TEETH_DEADZONE_HIGH, 0, 127, true);
                                    }
                                    bowPressureValue = (int)_teethMapper.Map(filteredTeethPressure);
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
