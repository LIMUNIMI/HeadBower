using HeadBower.Modules;
using NITHemulation.Modules.Keyboard;
using RawInputProcessor;

namespace HeadBower.OLD.Behaviors.Keyboard
{
    public class KBstopEmulateMouse : IKeyboardBehavior
    {
        private VKeyCodes keyAction = VKeyCodes.A;

        public int ReceiveEvent(RawInputEventArgs e)
        {
            if (e.VirtualKey == (ushort)keyAction)
            {
                Rack.GazeToMouse.Enabled = false;
                Rack.MappingModule.CursorHidden = false;
                Rack.InstrumentWindow.Cursor = Rack.MappingModule.CursorHidden ? System.Windows.Input.Cursors.None : System.Windows.Input.Cursors.Arrow;

                return 0;
            }

            return 1;
        }
    }
}