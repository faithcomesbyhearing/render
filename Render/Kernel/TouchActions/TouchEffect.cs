namespace Render.Kernel.TouchActions
{
    public class TouchEffect : RoutingEffect
    {
        public bool Capture { get; set; }

        public event TouchActionEventHandler TouchAction;

        public void OnTouchAction(Element element, TouchActionEventArgs args)
        {
            if (!Capture)
            {
                return;
            }

            TouchAction?.Invoke(element, args);
        }
    }
}