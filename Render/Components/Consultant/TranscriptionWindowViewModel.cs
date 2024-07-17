using System.Reactive;
using System.Text;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;

namespace Render.Components.Consultant
{
    public class TranscriptionWindowViewModel : ViewModelBase
    {
        private const double MinimumFontSize = 7.0;
        private readonly StringBuilder _stringBuilder = new();

        private readonly IViewModelContextProvider _viewModelContextProvider;
        private Section Section { get; set; }
        private double FontSize { get; set; }

        public readonly DynamicDataWrapper<PassageTranscriptionViewModel> Transcriptions = new();
        
        public ReactiveCommand<Unit, Unit> IncreaseFontSizeCommand { get; }
        public ReactiveCommand<Unit, Unit> DecreaseFontSizeCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyToClipBoardCommand { get; }

        
        [Reactive] public bool ClipBoardButtonIsClicked { get; private set; }

        public static async Task<TranscriptionWindowViewModel> CreateAsync(Section section,
            IViewModelContextProvider viewModelContextProvider)
        {
            var userMachineSettingsRepository = viewModelContextProvider.GetUserMachineSettingsRepository();
            var userId = viewModelContextProvider.GetLoggedInUser().Id;
            var userMachineSettings = await userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(userId);

            var fontSize = userMachineSettings.TranscribeFontSize;
            if (fontSize <= 0)
            {
                fontSize = 15.0;
            }

            return new TranscriptionWindowViewModel(section, fontSize, viewModelContextProvider);
        }

        private TranscriptionWindowViewModel(
            Section section, 
            double fontSize,
            IViewModelContextProvider viewModelContextProvider) : 
            base("TranscriptionWindow", viewModelContextProvider)
        {
            Section = section;
            FontSize = fontSize;

            _viewModelContextProvider = viewModelContextProvider;

            IncreaseFontSizeCommand = ReactiveCommand.Create(IncreaseFontSize);
            DecreaseFontSizeCommand = ReactiveCommand.Create(DecreaseFontSize);
            CopyToClipBoardCommand = ReactiveCommand.CreateFromTask(CopyToClipBoardAsync);
        }

        public void UpdateTranscriptions(bool isBackTranslate, bool isSegmentBackTranslate,
            bool isSecondStepBackTranslate)
        {
            Transcriptions.Clear();

            if (!isBackTranslate)
            {
                return;
            }

            if (isSegmentBackTranslate)
            {
                var segmentNumber = 1;

                foreach (var passage in Section.Passages)
                {
                    foreach (var audio in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
                    {
                        var transcription = isSecondStepBackTranslate
                            ? audio?.RetellBackTranslationAudio?.Transcription
                            : audio?.Transcription;

                        if (string.IsNullOrEmpty(transcription) is false)
                        {
                            Transcriptions.Add(new PassageTranscriptionViewModel(
                                number: $"S{segmentNumber}",
                                transcription: transcription,
                                fontSize: FontSize,
                                viewModelContextProvider: _viewModelContextProvider,
                                showPassageIcon: false));
                        }

                        segmentNumber++;
                    }
                }
            }
            else
            {
                foreach (var passage in Section.Passages)
                {
                    var transcription = isSecondStepBackTranslate
                        ? passage?.CurrentDraftAudio?.RetellBackTranslationAudio?.RetellBackTranslationAudio
                            ?.Transcription
                        : passage?.CurrentDraftAudio?.RetellBackTranslationAudio?.Transcription;

                    if (string.IsNullOrEmpty(transcription) is false)
                    {
                        Transcriptions.Add(new PassageTranscriptionViewModel(
                            number: passage.PassageNumber.PassageNumberString,
                            transcription: transcription,
                            fontSize: FontSize,
                            viewModelContextProvider: _viewModelContextProvider));
                    }
                }
            }
        }

        private async Task CopyToClipBoardAsync()
        {
            ClipBoardButtonIsClicked = !ClipBoardButtonIsClicked;
           
            foreach (var passageTranscription in Transcriptions.Items)
            {
                var pt = passageTranscription.Number + Environment.NewLine + passageTranscription.Transcription;
                _stringBuilder.AppendLine(pt);
                _stringBuilder.AppendLine("");
                passageTranscription.IsSelected = !passageTranscription.IsSelected;
            }
            
            await Clipboard.SetTextAsync(_stringBuilder.ToString());
            _stringBuilder.Clear();
        }
        
        private async void IncreaseFontSize()
        {
            FontSize += 2;
            foreach (var passageTranscription in Transcriptions.Items)
            {
                passageTranscription.FontSize = FontSize;
            }
            var userMachineSettingsRepository = ViewModelContextProvider.GetUserMachineSettingsRepository();
            var userMachineSettings = 
                await userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(ViewModelContextProvider.GetLoggedInUser().Id);
            userMachineSettings.SetFontSize(FontSize);
            await userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
        }
        
        private async void DecreaseFontSize()
        {
            if(FontSize <= MinimumFontSize) return;
            FontSize -= 2;
            foreach (var passageTranscription in Transcriptions.Items)
            {
                passageTranscription.FontSize = FontSize;
            }
            var userMachineSettingsRepository = ViewModelContextProvider.GetUserMachineSettingsRepository();
            var userMachineSettings = 
                await userMachineSettingsRepository.GetUserMachineSettingsForUserAsync(ViewModelContextProvider.GetLoggedInUser().Id);
            userMachineSettings.SetFontSize(FontSize);
            await userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
        }

        public override void Dispose()
        {
            Section = null;
            Transcriptions?.Dispose();


            base.Dispose();
        }
    }
}