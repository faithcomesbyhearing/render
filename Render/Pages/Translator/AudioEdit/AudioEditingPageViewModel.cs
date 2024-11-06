using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.MiniWaveformPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Audio;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Services.AudioServices;

namespace Render.Pages.Translator.AudioEdit
{
    public class AudioEditingPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly IAudioEncodingService _audioEncodingService;
        private readonly ITempAudioService _tempAudioService;

        [Reactive]
        public ISequencerEditorViewModel SequencerEditorViewModel { get; private set; }

        [Reactive]
        public IMiniWaveformPlayerViewModel MiniWaveformPlayerViewModel { get; private set; }

        public static AudioEditingPageViewModel Create(
            IViewModelContextProvider viewModelContextProvider,
            Section section, 
            Passage passage, 
            Stage stage, 
            Step step)
        {
            var color = ResourceExtensions.GetResourceValue<ColorReference>("SecondaryText");
            var pageGlyph = string.Empty;

            switch (step.RenderStepType)
            {
                case RenderStepTypes.PeerRevise :
                    pageGlyph = IconExtensions.BuildFontImageSource(Icon.PeerRevise, color.Color)?.Glyph;
                    break;
                case RenderStepTypes.CommunityRevise :
                    pageGlyph = IconExtensions.BuildFontImageSource(Icon.CommunityRevise, color.Color)?.Glyph;
                    break;
                case RenderStepTypes.ConsultantRevise :
                    pageGlyph = IconExtensions.BuildFontImageSource(Icon.ConsultantRevise, color.Color)?.Glyph;
                    break;
            }
            
            return new AudioEditingPageViewModel(viewModelContextProvider, section, passage, stage, step, pageGlyph);
        }
        
        private AudioEditingPageViewModel(
            IViewModelContextProvider viewModelContextProvider, 
            Section section, 
            Passage passage, 
            Stage stage, 
            Step step, 
            string pageGlyph) 
            : base("AudioEditingPage", 
                    viewModelContextProvider, 
                    GetStepName(step), section, stage, 
                    step, 
                    passage.PassageNumber, 
                    secondPageName: AppResources.DraftEdit)
        {
            _audioEncodingService = viewModelContextProvider.GetAudioEncodingService();
            _tempAudioService = viewModelContextProvider.GetTempAudioService(passage.CurrentDraftAudio);

            TitleBarViewModel.PageGlyph = pageGlyph;

            var barPlayerTitle = string.Format(AppResources.OriginalPassageTitle, passage.PassageNumber.PassageNumberString);
            MiniWaveformPlayerViewModel = viewModelContextProvider.GetMiniWaveformPlayerViewModel(
                passage.CurrentDraftAudio, ActionState.Optional, barPlayerTitle);

            ProceedButtonViewModel.SetCommand(NavigateToDraftingAsync);

            Disposables.Add(
                ProceedButtonViewModel
                .NavigateToPageCommand
                .IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));

            SetupSequencer(passage);
        }

        private void SetupSequencer(Passage passage)
        {
            SequencerEditorViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreateEditor(
                    playerFactory: ViewModelContextProvider.GetAudioPlayer,
                    recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000));

			SequencerEditorViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            SequencerEditorViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerEditorViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerEditorViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            SequencerEditorViewModel.InsertRecordCommand = ReactiveCommand.CreateFromTask<Unit, EditableAudioModel>(async (_) => 
            {
                var popup = new AudioInsertPageViewModel(ViewModelContextProvider);
                var path = await popup.ShowPopupAsync();
                popup.Dispose();

                if(string.IsNullOrEmpty(path) == false)
                {
                    return EditableAudioModel.Create(path);
                }

                return null;
            });

            var audio = passage.CurrentDraftAudio.CreateEditableAudioModel(
                            path: _tempAudioService.SaveTempAudio(),
                            key: passage.CurrentDraftAudio.Id);

            SequencerEditorViewModel.SetRecord(audio);

			SequencerEditorViewModel
			   .WhenAnyValue(player => player.State)
                .Subscribe(state => { 
                    if (state == SequencerState.Recording)
                    {
						ProceedButtonViewModel.ProceedActive = false;
					}
                    else
                    {
						ProceedButtonViewModel.ProceedActive =
                            SequencerEditorViewModel.TotalDuration != 0;
					}
                });
        }

        private async Task<IRoutableViewModel> NavigateToDraftingAsync()
        {
            var originalPath = _tempAudioService.SaveTempAudio(); 
            var record = await Task.Run(SequencerEditorViewModel.GetRecord);

            if (string.IsNullOrEmpty(record.Path) || record.Path == originalPath)
            {
                return await NavigateBack();
            }

            using var fileStream = new FileStream(record.Path, FileMode.Open, FileAccess.Read);

            var audioDetails = SequencerEditorViewModel.AudioDetails;
            var data = _audioEncodingService.ConvertWavToOpus(
                wavStream: fileStream,
                sampleRate: audioDetails.SampleRate,
                channelCount: audioDetails.ChannelCount);

            var audio = new Audio(default, default, default);
            audio.SetAudio(data);
            audio.SavedDuration = SequencerEditorViewModel.TotalDuration;

            var vm = await Task.Run(() => Task.FromResult((DraftingViewModel)HostScreen.Router.NavigationStack.GetPreviousScreen()));
            var loaded = await vm.LoadEditedAudioAsync(audio, record.Path);

            return loaded ? await NavigateBack() : null;
        }

        public override void Dispose()
        {
            _tempAudioService?.Dispose();

            MiniWaveformPlayerViewModel?.Dispose();
            SequencerEditorViewModel.Dispose();
            SequencerEditorViewModel = null;

            base.Dispose();
        }
    }
}