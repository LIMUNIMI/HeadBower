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
    /// 
    /// IMPORTANT: When using mouth aperture as the modulation source, this behavior only 
    /// processes frames that contain mouth_ape data. Frames from other sensors (e.g., eye tracker)
    /// that don't include facial parameters are skipped, preserving the last modulation value.
    /// </summary>
    public class ModulationControlBehavior : INithSensorBehavior
    {
        // Filters for each input source
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);

        // Constants for mouth aperture (0-100 percentage range from webcam wrapper)
        private const double MOUTH_APERTURE_THRESHOLD = 15.0;
        private const double MOUTH_APERTURE_MAX = 80.0; // Reach max modulation at 80% aperture, not 100%

        public void HandleData(NithSensorData nithData)
        {
            // Only apply modulation when modulation is enabled (independent of Blow state)
            if (Rack.UserSettings.ModulationControlMode != _ModulationControlModes.On)
            {
                Rack.MappingModule.Modulation = 0;
                return;
            }

            // Determine which source requires which parameters
            bool needsMouthApe = Rack.UserSettings.ModulationControlSource == ModulationControlSources.MouthAperture;
            bool needsPitch = Rack.UserSettings.ModulationControlSource == ModulationControlSources.HeadPitch;

            // SKIP THIS FRAME if required parameters are missing
            // This prevents flickering when using mouth aperture source with eye tracker
            // (eye tracker sends head motion but not facial parameters)
            if (needsMouthApe && !nithData.ContainsParameter(NithParameters.mouth_ape))
            {
                return; // Skip processing, keep last modulation value
            }

            if (needsPitch && !nithData.ContainsParameter(NithParameters.head_pos_pitch))
            {
                return; // Skip processing, keep last modulation value
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

            // Get mouth aperture (comes as 0-100 from webcam wrapper)
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
                    // Use threshold and range from settings
                    double pitchThreshold = Rack.UserSettings.PitchThreshold;
                    double maxPitchDeviation = Rack.UserSettings.PitchRange;
                    
                    // Deadzone: if within threshold, modulation stays at 0
                    if (Math.Abs(filteredPitch) <= pitchThreshold)
                    {
                        modulationValue = 0;
                    }
                    else
                    {
                        // Beyond threshold: map absolute pitch to 0-127
                        // Whether pitch is positive or negative, modulation increases
                        double absPitch = Math.Abs(filteredPitch);
                        var mapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 0, 127, true);
                        modulationValue = (int)mapper.Map(absPitch);
                    }
                    break;

                case ModulationControlSources.MouthAperture:
                    // Mouth aperture comes as 0-100 percentage from webcam wrapper
                    // Reaches max modulation at 80% aperture, giving headroom for natural mouth opening variations
                    if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                    {
                        modulationValue = 0;
                    }
                    else
                    {
                        var mapper = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MOUTH_APERTURE_MAX, 0, 127, true);
                        modulationValue = (int)mapper.Map(filteredMouthAperture);
                    }
                    break;
            }

            Rack.MappingModule.Modulation = modulationValue;
        }
    }
}
