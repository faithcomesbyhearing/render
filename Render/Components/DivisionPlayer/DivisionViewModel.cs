using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Render.Components.DivisionPlayer
{
    public class DivisionViewModel : ReactiveObject
    {
        [Reactive]
        public int ChunkDuration { get; private set; }

        [Reactive]
        public string Text { get; private set; }

        [Reactive]
        public double Scale { get; set; }
        
        [Reactive]
        public double LeftMargin { get; set; }
        
        [Reactive]
        public double RightMargin { get; set; }
        
        public DivisionViewModel(int chunkDuration, string text, double scale)
        {
            ChunkDuration = chunkDuration;
            Text = text;
            Scale = scale;
        }

        public void UpdateLabel(double leftMargin, double rightMargin)
        {
            LeftMargin = leftMargin;
            RightMargin = rightMargin;
        }
    }
}