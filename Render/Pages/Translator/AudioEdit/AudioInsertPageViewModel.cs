using System.Reactive;
using ReactiveUI;
using Render.Components.AudioRecorder;
using Render.Kernel;
using Render.Models.Audio;

namespace Render.Pages.Translator.AudioEdit
{
    public class AudioInsertPageViewModel : PopupViewModelBase<string>
    {
        public IMiniAudioRecorderViewModel MiniAudioRecorderViewModel { get; set; }

        public ReactiveCommand<Unit, Unit> InsertAudioCommand { get; }
        public ReactiveCommand<Unit, Unit> ClosePopupCommand { get; }
        
        public bool HasAutoCloseByBackgroundClick { get; private set; }

        public AudioInsertPageViewModel(IViewModelContextProvider viewModelContextProvider) : base("AudioInsert", viewModelContextProvider)
        {
            MiniAudioRecorderViewModel = viewModelContextProvider
                .GetMiniAudioRecorderViewModel(new Audio(default, default, default));

            InsertAudioCommand = ReactiveCommand.Create(InsertAudio);
            ClosePopupCommand = ReactiveCommand.Create(ClosePopup);
        }

        private void InsertAudio()
        {
            ClosePopup(MiniAudioRecorderViewModel.AudioFilePath);
        }

        private void ClosePopup()
        {
            ClosePopup(null);
        }

        public override void Dispose()
        {
            MiniAudioRecorderViewModel?.Dispose();
            MiniAudioRecorderViewModel = null;

            base.Dispose();
        }
    }
}