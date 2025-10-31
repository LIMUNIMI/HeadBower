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

        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants for mouth aperture (0-100 percentage range from webcam wrapper)
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double MOUTH_APERTURE_MAX = 80.0;
        
        // Pre-create mappers to avoid allocations every frame
        private SegmentMapper _pitchMapper;
        private SegmentMapper _mouthMapper;
        private double _lastPitchThreshold = 0;
        private double _lastPitchRange = 0;

        public void HandleData(NithSensorData nithData)
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
            List<NithParameters> requiredParams = currentSource == ModulationControlSources.HeadPitch 
                ? requiredParamsPitch 
                : requiredParamsMouth;

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
                }

                Rack.MappingModule.Modulation = modulationValue;
            }
            // If parameters missing, keep last modulation value (don't update)
        }
    }
}
