using HeadBower.Modules;
using NITHemulation.Modules.Keyboard;
using RawInputProcessor;

namespace HeadBower.OLD.Behaviors.Keyboard
{
    public class KBsimulateBlow : IKeyboardBehavior
    {
        private VKeyCodes keyBlow = VKeyCodes.Space;

        private bool blowing = false;
        int returnVal = 0;

        public int ReceiveEvent(RawInputEventArgs e)
        {
            returnVal = 0;

            if(Rack.UserSettings.InteractionMethod == InteractionMappings.Keyboard)
            {
                if (e.VirtualKey == (ushort)keyBlow && e.KeyPressState == KeyPressState.Down)
                {
                    blowing = true;
                    returnVal = 1;
                    Rack.MappingModule.InputIndicatorValue = 127;
                    Rack.MappingModule.Velocity = 127;
                    Rack.MappingModule.Pressure = 127;
                }
                else if (e.VirtualKey == (ushort)keyBlow && e.KeyPressState == KeyPressState.Up)
                {
                    blowing = false;
                    returnVal = 1;
                    Rack.MappingModule.InputIndicatorValue = 0;
                    Rack.MappingModule.Velocity = 0;
                    Rack.MappingModule.Pressure = 0;
                }
                Rack.MappingModule.Blow = blowing;
            }

            return returnVal;
        }
    }
}
