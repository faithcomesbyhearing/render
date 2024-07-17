namespace Render.Kernel.DragAndDrop
{
    public class DragAndDropEventArgs : EventArgs
    {
        public object Data { get; set; }

        public DragAndDropEventArgs() { }

        public DragAndDropEventArgs(object data)
        {
            Data = data;
        }
    }
}