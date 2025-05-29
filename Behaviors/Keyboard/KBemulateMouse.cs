using HeadBower.Modules;
using NITHemulation.Modules.Keyboard;
using RawInputProcessor;

namespace HeadBower.OLD.Behaviors.Keyboard
{
    public class KBemulateMouse : IKeyboardBehavior
    {
        private VKeyCodes keyAction = VKeyCodes.Q;

        public int ReceiveEvent(RawInputEventArgs e)
        {
            if (e.VirtualKey == (ushort)keyAction)
            {
                Rack.GazeToMouse.Enabled = true;
                Rack.MappingModule.CursorHidden = true;
                Rack.InstrumentWindow.Cursor = Rack.MappingModule.CursorHidden ? System.Windows.Input.Cursors.None : System.Windows.Input.Cursors.Arrow;

                return 0;
            }

            return 1;
        }
    }
}