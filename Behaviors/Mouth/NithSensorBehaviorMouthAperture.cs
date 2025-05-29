using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.OLD.Behaviors.Mouth
{
    
    internal class NithSensorBehaviorMouthAperture : INithSensorBehavior
    {
        private const float LOWERTHRESH = 15f;
        private const float UPPERTHRESH = 30f;

        private readonly NithParameters associatedParameter = NithParameters.mouth_ape;

        private readonly SegmentMapper inputMapper;
        private readonly IDoubleFilter inputFilter;

        private readonly float modulationDivider;
        private readonly float pressureMultiplier;
        private readonly float sensitivity;

        public NithSensorBehaviorMouthAperture(float sensitivity = 1f, float pressureMultiplier = 1f, float modulationDivider = 0.125f
            )
        {
            this.modulationDivider = modulationDivider;
            this.pressureMultiplier = pressureMultiplier;
            this.sensitivity = sensitivity;

            // Initialize mapper
            inputMapper = new SegmentMapper(0, 100, 0, 127);

            inputFilter = new DoubleFilterMAexpDecaying(0.5f);
        }

        // All incoming values from NithModuleBreathSensor are passed to this method as NithSensorData.
        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameter(associatedParameter))
            {
                var input = nithData.GetParameterValue(associatedParameter).Value.Normalized;

                input *= sensitivity; // Apply Sensitivity
                if (input > 100) input = 100; // Maximum threshold of 100
                inputFilter.Push(input);
                input = inputFilter.Pull();
                input = inputMapper.Map(input); // Map to MIDI range 0-127
                Rack.MappingModule.Pressure = (int)(input * pressureMultiplier); // Update instrument logic by changing MIDI channel pressure
                Rack.MappingModule.Modulation = (int)(input / modulationDivider); // Change modulation
                Rack.MappingModule.InputIndicatorValue = input; // Provide "raw" input value to the instrument's logic module to update a graphical indicator

                // Check the double threshold to determine whether to send a note on/note off (the "Blow" in the instrument's logic)
                if ((int)input > UPPERTHRESH && !Rack.MappingModule.Blow) Rack.MappingModule.Blow = true;

                if ((int)input <= LOWERTHRESH) Rack.MappingModule.Blow = false;
            }
        }
    }
}