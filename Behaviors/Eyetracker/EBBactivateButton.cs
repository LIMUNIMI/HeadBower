using System.Windows;
using System.Windows.Controls.Primitives;
using HeadBower.Modules;
using NITHlibrary.Nith.BehaviorTemplates;

namespace HeadBower.OLD.Behaviors.Eyetracker
{
    public class EBBactivateButton : ANithBlinkEventBehavior
    {
        public EBBactivateButton()
        {
            DCThresh = 4;
        }

        protected override void Event_doubleClose()
        {
            if(Rack.UserSettings.InteractionMethod == InteractionMappings.EyePos)
            {
                if (Rack.MappingModule.HasAButtonGaze)
                {
                    Rack.MappingModule.LastGazedButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
            }
        }

        protected override void Event_doubleOpen() { }

        protected override void Event_leftClose() { }

        protected override void Event_leftOpen() { }

        protected override void Event_rightClose() { }

        protected override void Event_rightOpen() { }
    }
}
