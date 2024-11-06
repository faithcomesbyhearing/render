namespace Render.Kernel.DragAndDrop
{
    public class DragAndDropEventArgs : EventArgs
    {
        public bool IsDragEnded { get; set; }

        public object Data { get; set; }

        public DragAndDropEventArgs() { }

        public DragAndDropEventArgs(object data)
        {
            Data = data;
        }
    }
}