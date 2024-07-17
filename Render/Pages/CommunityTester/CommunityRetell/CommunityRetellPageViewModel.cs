using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Resources.Localization;
using Render.Resources;
using Render.Resources.Styles;
using Render.Sequencer;
using Render.Sequencer.Contracts.Interfaces;
using Render.Services.AudioServices;
using Render.Services.SessionStateServices;
using DynamicData;
using Render.Extensions;
using Render.Models.Audio;
using Render.Sequencer.Contracts.Models;
using Render.Repositories.SectionRepository;
using Render.Pages.CommunityTester.CommunityQAndR;
using Render.Sequencer.Contracts.Enums;

namespace Render.Pages.CommunityTester.CommunityRetell
{
    public class CommunityRetellPageViewModel : WorkflowPageBaseViewModel
    {
        public IMiniWaveformPlayerViewModel DraftPassagePlayerViewModel { get; private set; }
        public IMiniWaveformPlayerViewModel SectionPlayerViewModel { get; private set; }
        [Reactive] public ISequencerRecorderViewModel SequencerRecorderViewModel { get; private set; }
        public ActionViewModelBase SequencerActionViewModel { get; private set; }
        private IAudioRepository<Models.Sections.CommunityCheck.CommunityRetell> _audioRepository;
        private readonly IAudioEncodingService _audioEncodingService;
        private Step _step;
        private Models.Sections.CommunityCheck.CommunityRetell _communityRetell;
        private CommunityTest _communityCheck;
        private ICommunityTestRepository _communityTestRepository;

        public CommunityRetellPageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Passage passage,
            Stage stage,
            Step step,
            bool isSectionAudioListened = false) : base(
                urlPathSegment: "CommunityRetellPage",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(viewModelContextProvider, RenderStepTypes.CommunityTest, stage.Id),
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                secondPageName: AppResources.PassageRetell)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            var color = (ColorReference)ResourceExtensions.GetResourceValue("SecondaryText");
            if (color != null)
                TitleBarViewModel.PageGlyph = IconExtensions.BuildFontImageSource(Icon.CommunityCheck, color.Color, 35)?.Glyph;
            _step = step;

            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));
            _audioRepository = viewModelContextProvider.GetCommunityRetellRepository();
            _audioEncodingService = viewModelContextProvider.GetAudioEncodingService();
            _communityTestRepository = viewModelContextProvider.GetCommunityTestRepository();

            InitializeMiniPlayers(viewModelContextProvider, passage, step, section, isSectionAudioListened);

            //Audio recorder
            _communityCheck = passage.CurrentDraftAudio.GetCommunityCheck();
            var gcs = viewModelContextProvider.GetGrandCentralStation();
            var stageId = gcs.ProjectWorkflow.GetStage(step.Id).Id;
            _communityRetell = _communityCheck.RetellsAllStages.FirstOrDefault(x => x.StageId == stageId);
            if (_communityRetell == null)
            {
                _communityRetell = new Models.Sections.CommunityCheck.CommunityRetell(stageId, section.ScopeId, section.ProjectId, _communityCheck.Id);
                _communityCheck.AddRetell(_communityRetell);
            }

            SequencerRecorderViewModel = ViewModelContextProvider.GetSequencerFactory().CreateRecorder(
                playerFactory: ViewModelContextProvider.GetAudioPlayer,
                recorderFactory: () => ViewModelContextProvider.GetAudioRecorderFactory().Invoke(48000));
            
            SequencerRecorderViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;
            SequencerRecorderViewModel.AllowAppendRecordMode = true;

            SequencerRecorderViewModel.SetupActivityService(ViewModelContextProvider, Disposables);
            SequencerRecorderViewModel.SetupRecordPermissionPopup(ViewModelContextProvider, Logger);
            SequencerRecorderViewModel.SetupOnRecordFailedPopup(ViewModelContextProvider, Logger);

            SequencerRecorderViewModel.OnDeleteRecordCommand = ReactiveCommand.Create(DeleteRetellAudio);
            SequencerRecorderViewModel.OnUndoDeleteRecordCommand = ReactiveCommand.Create(SaveRetellAudio);
            SequencerRecorderViewModel.OnRecordingFinishedCommand = ReactiveCommand.Create(SaveRetellAudio);

            SequencerActionViewModel = SequencerRecorderViewModel.CreateActionViewModel(ViewModelContextProvider, Disposables, required: true);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            if (_communityRetell.Data.Length is not 0)
            {
                SequencerRecorderViewModel.SetRecord(RecordAudioModel.Create(
                    path: ViewModelContextProvider.GetTempAudioService(_communityRetell).SaveTempAudio(),
                    name: string.Empty));
                SequencerActionViewModel.ActionState = ActionState.Optional;
            }
            SequencerRecorderViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Recording)
                .Subscribe((state) => { SequencerActionViewModel.ActionState = ActionState.Required; });

            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            var passage = Section.Passages.First(x => x.PassageNumber.Equals(PassageNumber));
            var nextPassage = Section.GetNextPassage(passage);
            if (nextPassage == null)
            {
                if (!_step.StepSettings.GetSetting(SettingType.DoCommunityResponse))
                {
                    ProceedButtonViewModel.IsCheckMarkIcon = true;
                }
            }
        }

        private void InitializeMiniPlayers(
            IViewModelContextProvider viewModelContextProvider,
            Passage passage,
            Step step,
            Section section,
            bool isSectionAudioListened)
        {
            var stepSetting = step.StepSettings.GetSetting(SettingType.RequirePassageListen);

            DraftPassagePlayerViewModel = viewModelContextProvider.GetMiniWaveformPlayerViewModel(
                audio: passage.CurrentDraftAudio,
                actionState: stepSetting ? ActionState.Required : ActionState.Optional,
                title: string.Format(AppResources.PassageNumber, passage.PassageNumber.PassageNumberString));

            var requireSectionListen = step.StepSettings.GetSetting(SettingType.RequireSectionListen);
            SectionPlayerViewModel = viewModelContextProvider.GetMiniWaveformPlayerViewModel(
                audio: new AudioPlayback(section.Id,  section.Passages.Select(x => x.CurrentDraftAudio)),
                title: string.Format(AppResources.Section, section.Number),
                actionState: requireSectionListen && isSectionAudioListened is false ?
                    ActionState.Required :
                    ActionState.Optional);

            ActionViewModelBaseSourceList.Add(DraftPassagePlayerViewModel);
            ActionViewModelBaseSourceList.Add(SectionPlayerViewModel);
        }

        private async void SaveRetellAudio()
        {
            IsLoading = true;

            await Task.Run(async () =>
            {
                var record = SequencerRecorderViewModel.GetRecord();
                var audioDetails = SequencerRecorderViewModel.AudioDetails;
                var audioStream = (Stream)null;
                if (record.HasData)
                {
                    audioStream = new MemoryStream(record.Data);
                }
                else if (record.IsFileExists)
                {
                    audioStream = new FileStream(record.Path, FileMode.Open, FileAccess.Read);
                }
                else
                {
                    //TODO: Handle empty audio, show error to user.
                    throw new InvalidOperationException("Audio does not contain data or temporary file path");
                }

                await using (audioStream)
                {
                    var audioData = ViewModelContextProvider.GetAudioEncodingService().ConvertWavToOpus(
                        wavStream: audioStream,
                        sampleRate: audioDetails.SampleRate,
                        channelCount: audioDetails.ChannelCount);
                    _communityRetell.SetAudio(audioData);
                    await _audioRepository.SaveAsync(_communityRetell);
                    await _communityTestRepository.SaveCommunityTestAsync(_communityCheck);
                }
            });
            SequencerActionViewModel.ActionState = ActionState.Optional;
            IsLoading = false;
        }

        private void DeleteRetellAudio()
        {
            Task.Factory.StartNew(async () =>
            {
                await _audioRepository.DeleteAudioByIdAsync(_communityRetell.Id);
            });
            SequencerActionViewModel.ActionState = ActionState.Required;
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            var passage = Section.Passages.First(x => x.PassageNumber.Equals(PassageNumber));

            if (_step.StepSettings.GetSetting(SettingType.DoCommunityResponse))
            {
                var vm = await Task.Run(() => CommunityQAndRPageViewModel.Create(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: Section,
                    passage: passage,
                    stage: Stage,
                    step: _step,
                    isSectionAudioListened: true));
                return await NavigateTo(vm);
            }

            var nextPassage = Section.GetNextPassage(passage);
            if (nextPassage != null)
            {
                var vm = await Task.Run(() => new CommunityRetellPageViewModel(
                    viewModelContextProvider: ViewModelContextProvider,
                    section: Section,
                    passage: nextPassage,
                    stage: Stage,
                    step: _step,
                    isSectionAudioListened: true));

                return await NavigateTo(vm);
            }

            var gcs = ViewModelContextProvider.GetGrandCentralStation();
            await Task.Run(async () => { await gcs.AdvanceSectionAsync(Section, _step); });
            return await NavigateToHomeOnMainStackAsync();
        }

        public override void Dispose()
        {
            _communityRetell = null;
            _communityCheck = null;
            _step = null;
            DraftPassagePlayerViewModel?.Dispose();
            SectionPlayerViewModel?.Dispose();
            SequencerRecorderViewModel?.Dispose();
            SequencerRecorderViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _communityTestRepository?.Dispose();
            _audioRepository?.Dispose();

            base.Dispose();
        }
    }
}