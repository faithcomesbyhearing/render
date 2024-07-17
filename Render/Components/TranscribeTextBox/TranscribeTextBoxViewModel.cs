using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Components.TranscribeTextBox
{
    public class TranscribeTextBoxViewModel : ActionViewModelBase
    {
        private const double MinimumFontSize = 5.0;
        private const double MaximumFontSize = 48.0;

        [Reactive] public double FontSize { get; private set; }
        [Reactive] public string Input { get; set; }

        public ReactiveCommand<Unit,Unit> IncreaseFontSizeCommand { get; }
        public ReactiveCommand<Unit,Unit> DecreaseFontSizeCommand { get; }
        public ReactiveCommand<Unit,Unit> CopyTextCommand { get; }

        public static async Task<TranscribeTextBoxViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            string text)
        {
            var userMachineSettingsRepository = viewModelContextProvider
                .GetUserMachineSettingsRepository();

            var userMachineSettings =  await userMachineSettingsRepository
                .GetUserMachineSettingsForUserAsync(viewModelContextProvider.GetLoggedInUser().Id);

            var fontSize = userMachineSettings.TranscribeFontSize;
            if (fontSize <= 0)
            {
                fontSize = 10.0;
            }
         
            return new TranscribeTextBoxViewModel(fontSize, viewModelContextProvider, text);
        }

        private TranscribeTextBoxViewModel(
            double fontSize,
            IViewModelContextProvider viewModelContextProvider,
            string text) : 
            base(
                actionState: string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text) ? 
                    ActionState.Required : 
                    ActionState.Optional,
                urlPathSegment: "TranscriptionWindow",
                viewModelContextProvider: viewModelContextProvider)
        {
            FontSize = fontSize;
            Input = text;

            IncreaseFontSizeCommand = ReactiveCommand.Create(IncreaseFontSize);
            DecreaseFontSizeCommand = ReactiveCommand.Create(DecreaseFontSize);
            CopyTextCommand = ReactiveCommand.Create(CopyText);

            Disposables.Add(this
                .WhenAnyValue(x => x.Input)
                .Subscribe(s =>
                {
                    if (string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s))
                    {
                        ActionState = ActionState.Required;
                    }
                    else
                    {
                        ActionState = ActionState.Optional;
                    }
                }));
        }

        private async void IncreaseFontSize()
        {
            if (FontSize + 2 >= MaximumFontSize || Input == string.Empty)
            {
                return;
            }

            FontSize += 2;

            var userMachineSettingsRepository = ViewModelContextProvider.GetUserMachineSettingsRepository();
            var userMachineSettings = await userMachineSettingsRepository
                .GetUserMachineSettingsForUserAsync(ViewModelContextProvider.GetLoggedInUser().Id);

            userMachineSettings.SetFontSize(FontSize);
            await userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
        }
        
        private async void DecreaseFontSize()
        {
            if(FontSize - 2 <= MinimumFontSize || Input == string.Empty)
            {
                return;
            }

            FontSize -= 2;
            var userMachineSettingsRepository = ViewModelContextProvider.GetUserMachineSettingsRepository();
            var userMachineSettings = await userMachineSettingsRepository
                .GetUserMachineSettingsForUserAsync(ViewModelContextProvider.GetLoggedInUser().Id);

            userMachineSettings.SetFontSize(FontSize);
            await userMachineSettingsRepository.UpdateUserMachineSettingsAsync(userMachineSettings);
        }

        private async void CopyText()
        {
            if (Input == string.Empty)
            {
                return;
            }

            await Clipboard.SetTextAsync(Input);
        }
    }
}