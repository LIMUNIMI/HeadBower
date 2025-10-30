using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Controls MIDI modulation (CC1) based on various input sources:
    /// - Head pitch rotation
    /// - Mouth aperture
    /// - Head roll rotation
    /// Does NOT update visual feedback - that's handled by VisualFeedbackBehavior.
    /// </summary>
    public class ModulationControlBehavior : INithSensorBehavior
    {
        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _rollPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double MAX_ROLL_DEVIATION = 50.0;
        private const double ROLL_THRESHOLD = 15.0;

        public void HandleData(NithSensorData nithData)
        {
            // Only apply modulation when note is being played and modulation is enabled
            if (!Rack.MappingModule.Blow || Rack.UserSettings.ModulationControlMode != _ModulationControlModes.On)
            {
                Rack.MappingModule.Modulation = 0;
                return;
            }

            // Get and filter input values based on source
            double filteredPitch = 0;
            double filteredRoll = 0;
            double filteredMouthAperture = 0;

            // Get head pitch
            if (nithData.ContainsParameter(NithParameters.head_pos_pitch))
            {
                double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                _pitchPosFilter.Push(rawPitch);
                filteredPitch = _pitchPosFilter.Pull();
            }

            // Get head roll
            if (nithData.ContainsParameter(NithParameters.head_pos_roll))
            {
                double rawRoll = nithData.GetParameterValue(NithParameters.head_pos_roll).Value.ValueAsDouble;
                _rollPosFilter.Push(rawRoll);
                filteredRoll = _rollPosFilter.Pull();
            }

            // Get mouth aperture
            if (nithData.ContainsParameter(NithParameters.mouth_ape))
            {
                double rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                _mouthApertureFilter.Push(rawMouthAperture);
                filteredMouthAperture = _mouthApertureFilter.Pull();
            }

            // Calculate modulation value based on selected source
            int modulationValue = 0;

            switch (Rack.UserSettings.ModulationControlSource)
            {
                case ModulationControlSources.HeadPitch:
                    // Use threshold from settings
                    double pitchThreshold = Rack.UserSettings.PitchThreshold;
                    double maxPitchDeviation = Rack.UserSettings.PitchRange;
                    
                    if (Math.Abs(filteredPitch) <= pitchThreshold)
                    {
                        modulationValue = 0;
                    }
                    else
                    {
                        double absPitch = Math.Abs(filteredPitch);
                        var mapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 0, 127, true);
                        modulationValue = (int)mapper.Map(absPitch);
                    }
                    break;

                case ModulationControlSources.MouthAperture:
                    if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                    {
                        modulationValue = 0;
                    }
                    else
                    {
                        var mapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 0, 127, true);
                        modulationValue = (int)mapper.Map(filteredMouthAperture);
                    }
                    break;

                case ModulationControlSources.HeadRoll:
                    if (Math.Abs(filteredRoll) <= ROLL_THRESHOLD)
                    {
                        modulationValue = 0;
                    }
                    else
                    {
                        double absRoll = Math.Abs(filteredRoll);
                        var mapper = new SegmentMapper(ROLL_THRESHOLD, MAX_ROLL_DEVIATION, 0, 127, true);
                        modulationValue = (int)mapper.Map(absRoll);
                    }
                    break;
            }

            Rack.MappingModule.Modulation = modulationValue;
        }
    }
}
