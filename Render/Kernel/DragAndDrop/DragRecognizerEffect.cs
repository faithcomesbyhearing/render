namespace Render.Kernel.DragAndDrop
{
    public class DragRecognizerEffect : RoutingEffect
    {
        public event DragAndDropEventHandler DragStarting;

        public void SendDragStarting(Element element, DragAndDropEventArgs args)
        {
            DragStarting?.Invoke(element, args);
        }
    }
}