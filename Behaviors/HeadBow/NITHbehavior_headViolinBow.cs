using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Imitates a violin bow using head movements (pitch position and yaw acceleration).
    /// </summary>
    internal class NITHbehavior_headViolinBow : INithSensorBehavior
    {
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_pitch,
            NithParameters.head_acc_yaw
        };

        private readonly VelocityCalculatorFiltered _velocityCalculator = new VelocityCalculatorFiltered(2, 5);

        private bool _noteOn = false;

        public DoubleFilterMAexpDecaying ExpressionFilter { get; set; } = new DoubleFilterMAexpDecaying(0.3f);
        public SegmentMapper ExpressionMapper { get; set; } = new SegmentMapper(0, 65, 0, 127);
        private DoubleFilterMAexpDecaying BowPressureFilter { get; set; } = new DoubleFilterMAexpDecaying(0.4f);
        public SegmentMapper BowPressureMapper { get; set; } = new SegmentMapper(0, 25, 0, 127);

        // Soglia superiore per l'attivazione della nota
        public double YAW_THRESHOLD_NOTEON { get; set; } = 15f;
        // Soglia inferiore per la disattivazione della nota (creando una finestra di isteresi)
        public double YAW_THRESHOLD_NOTEOFF { get; set; } = 8f;

        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameters(requiredParams))
            {
                
                // Get the values of the parameters
                double head_pos_pitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                double head_acc_yaw = nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.ValueAsDouble;

                // Process yaw (expression, note on-off)
                _velocityCalculator.Push(head_acc_yaw); // calculate velocity
                ExpressionFilter.Push(head_acc_yaw);
                double expressionAbs = Math.Abs(ExpressionFilter.Pull());
                double expressionMapped = ExpressionMapper.Map(expressionAbs);
                
                Rack.MidiModule.SendControlChange(8, (int)expressionMapped);

                // Process pitch (bow pressure)
                BowPressureFilter.Push(head_pos_pitch);
                double bowPressureAbs = Math.Abs(BowPressureFilter.Pull());
                double bowPressureMapped = BowPressureMapper.Map(bowPressureAbs);
                Rack.MidiModule.SendControlChange(9, (int)bowPressureMapped);

                // Implementazione con doppia soglia (isteresi)
                // La nota viene attivata quando si supera YAW_THRESHOLD_NOTEON
                // La nota viene disattivata quando si scende sotto YAW_THRESHOLD_NOTEOFF
                if (expressionMapped > YAW_THRESHOLD_NOTEON && !_noteOn)
                {
                    if(head_pos_pitch < 0)
                    {
                        Rack.MidiModule.NoteOn(pitch: 60, velocity: (int)_velocityCalculator.PullInstantSpeed()); // Note on (C4)
                        _noteOn = true;
                    }
                    
                }
                else if (expressionMapped < YAW_THRESHOLD_NOTEOFF && _noteOn)
                {
                    _noteOn = false;
                    Rack.MidiModule.NoteOff(60); // Note off (C4)
                }
            }
        }
    }
}