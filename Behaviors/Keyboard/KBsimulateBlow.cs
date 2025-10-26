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
            // Obsolete Keyboard interaction removed - this behavior is no longer used
            return 0;
        }
    }
}
