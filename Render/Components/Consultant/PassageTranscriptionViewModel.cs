using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Components.Consultant
{
    public class PassageTranscriptionViewModel : ViewModelBase
    {
        public string Transcription { get; }
        public string Number { get; }
        
        [Reactive]
        public double FontSize { get; set; }
        
        [Reactive]
        public bool IsSelected { get; set; }
        
        [Reactive]
        public bool ShowPassageIcon { get; set; }
        
        public PassageTranscriptionViewModel(string number, string transcription, double fontSize,
            IViewModelContextProvider viewModelContextProvider, bool showPassageIcon = true) :
            base("PassageTranscription", viewModelContextProvider)
        {
            Number = number;
            Transcription = transcription;
            FontSize = fontSize;
            ShowPassageIcon = showPassageIcon;
        }
    }
}