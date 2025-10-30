using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Controls MIDI bow pressure (CC9) based on various input sources:
    /// - Head pitch rotation
    /// - Mouth aperture
    /// Does NOT update visual feedback - that's handled by VisualFeedbackBehavior.
    /// </summary>
    public class BowPressureControlBehavior : INithSensorBehavior
    {
        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants
        private const double MAX_MOUTH_APERTURE = 100.0;
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;

        public void HandleData(NithSensorData nithData)
        {
            // Only apply bow pressure when note is being played
            if (!Rack.MappingModule.Blow)
            {
                Rack.MidiModule.SendControlChange(9, 0);
                return;
            }

            // Get and filter input values based on source
            double filteredPitch = 0;
            double filteredMouthAperture = 0;

            // Get head pitch
            if (nithData.ContainsParameter(NithParameters.head_pos_pitch))
            {
                double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                _pitchPosFilter.Push(rawPitch);
                filteredPitch = _pitchPosFilter.Pull();
            }

            // Get mouth aperture
            if (nithData.ContainsParameter(NithParameters.mouth_ape))
            {
                double rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                _mouthApertureFilter.Push(rawMouthAperture);
                filteredMouthAperture = _mouthApertureFilter.Pull();
            }

            // Calculate bow pressure value based on selected source
            int bowPressureValue = 0;

            switch (Rack.UserSettings.BowPressureControlSource)
            {
                case BowPressureControlSources.HeadPitch:
                    // Use threshold from settings
                    double pitchThreshold = Rack.UserSettings.PitchThreshold;
                    double maxPitchDeviation = Rack.UserSettings.PitchRange;
                    
                    if (Math.Abs(filteredPitch) <= pitchThreshold)
                    {
                        bowPressureValue = 0;
                    }
                    else
                    {
                        double absPitch = Math.Abs(filteredPitch);
                        var mapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 0, 127, true);
                        bowPressureValue = (int)mapper.Map(absPitch);
                    }
                    break;

                case BowPressureControlSources.MouthAperture:
                    if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                    {
                        bowPressureValue = 0;
                    }
                    else
                    {
                        var mapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 0, 127, true);
                        bowPressureValue = (int)mapper.Map(filteredMouthAperture);
                    }
                    break;
            }

            Rack.MidiModule.SendControlChange(9, bowPressureValue);
        }
    }
}
