using HeadBower.Modules;
using NITHdmis.Music;
using NITHemulation.Modules.Keyboard;
using RawInputProcessor;

namespace HeadBower.OLD.Behaviors.Keyboard
{
    class KBselectScale : IKeyboardBehavior
    {
        private const VKeyCodes keyMaj = VKeyCodes.Add;
        private const VKeyCodes keyMin = VKeyCodes.Subtract;

        public int ReceiveEvent(RawInputEventArgs e)
        {
            if (e.VirtualKey == (ushort)keyMaj && e.KeyPressState == KeyPressState.Down)
            {
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.MappingModule.NetytarSurface.CheckedButton.Note.ToAbsNote(), ScaleCodes.maj);
                return 1;
            }
            if (e.VirtualKey == (ushort)keyMaj && e.KeyPressState == KeyPressState.Up)
            {
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.MappingModule.NetytarSurface.CheckedButton.Note.ToAbsNote(), ScaleCodes.min);
            };
            return 0;
        }
    }
}
