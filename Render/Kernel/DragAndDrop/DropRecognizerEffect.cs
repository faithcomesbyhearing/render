namespace Render.Kernel.DragAndDrop
{
    public class DropRecognizerEffect : RoutingEffect
    {
        public string DropText { get; set; }

        public event DragAndDropEventHandler DragLeave;

        public event DragAndDropEventHandler DragOver;

        public event DragAndDropEventHandler Drop;

        public void SendDragLeave(Element element, DragAndDropEventArgs args)
        {
            DragLeave?.Invoke(element, args);
        }

        public void SendDragOver(Element element, DragAndDropEventArgs args)
        {
            DragOver?.Invoke(element, args);
        }

        public void SendDrop(Element element, DragAndDropEventArgs args)
        {
            Drop?.Invoke(element, args);
        }
    }
}