using HeadBower.Modules;
using NITHlibrary.Nith.BehaviorTemplates;

namespace HeadBower.OLD.Behaviors.Eyetracker
{
    public class EBBrepeatNote : ANithBlinkEventBehavior
    {
        public EBBrepeatNote()
        {
            DCThresh = 4;
        }

        protected override void Event_doubleClose()
        {
            if (Rack.MappingModule.Blow)
            {
                Rack.MappingModule.Blow = false;
                Rack.MappingModule.Blow = true;
                //NetytarRack.DMIBox.NetytarSurface.FlashSpark();
            }
        }

        protected override void Event_doubleOpen() { }

        protected override void Event_leftClose() { }

        protected override void Event_leftOpen() { }

        protected override void Event_rightClose() { }

        protected override void Event_rightOpen() { }
    }
}
