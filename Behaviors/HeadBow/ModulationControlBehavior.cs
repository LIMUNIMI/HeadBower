using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Controls MIDI modulation (CC1) based on various input sources:
    /// - Head pitch rotation (with deadzone)
    /// - Mouth aperture (percentage 0-100)
    /// - Breath pressure (0-100 normalized, mapped to 0-127 with deadzones at 10 and 90)
    /// - Teeth pressure (0-100 normalized, mapped to 0-127 with deadzones at 10 and 90)
    /// Does NOT update visual feedback - that's handled by VisualFeedbackBehavior.
    /// </summary>
    public class ModulationControlBehavior : INithSensorBehavior
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

        // Constants for mouth aperture (0-100 percentage range from webcam wrapper)
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double MOUTH_APERTURE_MAX = 80.0;
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
        private double _lastPitchRange = 0;

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Only apply modulation when enabled
                if (Rack.UserSettings.ModulationControlMode != _ModulationControlModes.On)
                {
                    Rack.MappingModule.Modulation = 0;
                    return;
                }

                // Determine which source we're using and check if ALL required parameters are present
                ModulationControlSources currentSource = Rack.UserSettings.ModulationControlSource;
                
                // Select appropriate parameter list based on source
                List<NithParameters> requiredParams = currentSource switch
                {
                    ModulationControlSources.HeadPitch => requiredParamsPitch,
                    ModulationControlSources.MouthAperture => requiredParamsMouth,
                    ModulationControlSources.BreathPressure => requiredParamsBreath,
                    ModulationControlSources.TeethPressure => requiredParamsTeeth,
                    _ => requiredParamsPitch
                };

                // ONLY process if ALL required parameters are present
                if (nithData.ContainsParameters(requiredParams))
                {
                    int modulationValue = 0;

                    switch (currentSource)
                    {
                        case ModulationControlSources.HeadPitch:
                            // Get and filter pitch
                            double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                            _pitchPosFilter.Push(rawPitch);
                            double filteredPitch = _pitchPosFilter.Pull();

                            // Use threshold from settings
                            double pitchThreshold = Rack.UserSettings.PitchThreshold;
                            double maxPitchDeviation = Rack.UserSettings.PitchRange;
                            
                            if (Math.Abs(filteredPitch) <= pitchThreshold)
                            {
                                modulationValue = 0;
                            }
                            else
                            {
                                // Recreate mapper only if thresholds changed
                                if (_pitchMapper == null || pitchThreshold != _lastPitchThreshold || maxPitchDeviation != _lastPitchRange)
                                {
                                    _pitchMapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 0, 127, true);
                                    _lastPitchThreshold = pitchThreshold;
                                    _lastPitchRange = maxPitchDeviation;
                                }
                                double absPitch = Math.Abs(filteredPitch);
                                modulationValue = (int)_pitchMapper.Map(absPitch);
                            }
                            break;

                        case ModulationControlSources.MouthAperture:
                            // Get and filter mouth aperture
                            double rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                            _mouthApertureFilter.Push(rawMouthAperture);
                            double filteredMouthAperture = _mouthApertureFilter.Pull();

                            if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                            {
                                modulationValue = 0;
                            }
                            else
                            {
                                if (_mouthMapper == null)
                                {
                                    _mouthMapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MOUTH_APERTURE_MAX, 0, 127, true);
                                }
                                modulationValue = (int)_mouthMapper.Map(filteredMouthAperture);
                            }
                            break;

                        case ModulationControlSources.BreathPressure:
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
                                    modulationValue = 0;
                                }
                                else if (filteredBreathPressure >= BREATH_DEADZONE_HIGH)
                                {
                                    modulationValue = 127;
                                }
                                else
                                {
                                    // Map from 10-90 range to 0-127
                                    if (_breathMapper == null)
                                    {
                                        _breathMapper = new SegmentMapper(BREATH_DEADZONE_LOW, BREATH_DEADZONE_HIGH, 0, 127, true);
                                    }
                                    modulationValue = (int)_breathMapper.Map(filteredBreathPressure);
                                }
                            }
                            break;

                        case ModulationControlSources.TeethPressure:
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
                                    modulationValue = 0;
                                }
                                else if (filteredTeethPressure >= TEETH_DEADZONE_HIGH)
                                {
                                    modulationValue = 127;
                                }
                                else
                                {
                                    // Map from 10-90 range to 0-127
                                    if (_teethMapper == null)
                                    {
                                        _teethMapper = new SegmentMapper(TEETH_DEADZONE_LOW, TEETH_DEADZONE_HIGH, 0, 127, true);
                                    }
                                    modulationValue = (int)_teethMapper.Map(filteredTeethPressure);
                                }
                            }
                            break;
                    }

                    Rack.MappingModule.Modulation = modulationValue;
                }
                // If parameters missing, keep last modulation value (don't update)
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"ModulationControlBehavior Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                // Set to zero on error
                try { Rack.MappingModule.Modulation = 0; } catch { }
            }
        }
    }
}
