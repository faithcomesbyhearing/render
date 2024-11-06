namespace Render.Kernel.DragAndDrop
{
    public class DragRecognizerEffect : RoutingEffect
    {
        public event DragAndDropEventHandler DragStarting;
        public event DragAndDropEventHandler DragEnded;

        public void SendDragStarting(Element element, DragAndDropEventArgs args)
        {
            DragStarting?.Invoke(element, args);
        }

        public void SendDragEnded(Element element, DragAndDropEventArgs args)
        {
            DragEnded?.Invoke(element, args);
        }
    }
}