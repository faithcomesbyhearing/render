using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources.Localization;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Resources;
using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.BackTranslator.SegmentBackTranslate
{
    public class TabletSegmentSelectPageViewModel : WorkflowPageBaseViewModel
    {
        private readonly Dictionary<Guid, string> _segmentsDictionary = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, Guid> _segmentAudioSegmentBackTranslationDictionary = new Dictionary<Guid, Guid>();
        private readonly Dictionary<Guid, Guid> _segmentBackTranslationPassageDictionary = new Dictionary<Guid, Guid>();

        private Passage _passage;
        private SegmentBackTranslation _selectedSegment;

        private ActionViewModelBase SequencerActionViewModel { get; set; }

        private bool IsSecondBackTranslation
        {
            get => Step?.Role is Roles.BackTranslate2;
        }

        [Reactive] 
        public ISequencerPlayerViewModel SequencerPlayerViewModel { get; private set; }

        public static async Task<TabletSegmentSelectPageViewModel> CreateAsync(
            Step step,
            Section section,
            Passage passage,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            SegmentBackTranslation selectedSegment = null)
        {
            var pageVm = new TabletSegmentSelectPageViewModel(
                step: step,
                section: section,
                passage: passage,
                viewModelContextProvider: viewModelContextProvider,
                stage: stage,
                selectedSegment: selectedSegment);

            await pageVm.Initialize();

            return pageVm;
        }

        private TabletSegmentSelectPageViewModel(
            Step step,
            Section section,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            Passage passage,
            SegmentBackTranslation selectedSegment = null)
            : base(
                urlPathSegment: "TabletSegmentSelect",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                isSegmentSelect: true,
                passageNumber: passage.PassageNumber,
                secondPageName: AppResources.SegmentSelect)
        {
            _passage = passage;
            _selectedSegment = selectedSegment;
            
            TitleBarViewModel.PageGlyph = IconExtensions
                .BuildFontImageSource(Icon.SegmentBackTranslate, ResourceExtensions.GetColor("SecondaryText"))?.Glyph;

            ProceedButtonViewModel.SetCommand(NavigateToSelectedBreathPauseSegmentAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        private async Task Initialize()
        {
            await SetupSequencer();
        }

        private async Task SetupSequencer()
        {
            SequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreatePlayer(ViewModelContextProvider.GetAudioPlayer);

            SequencerPlayerViewModel.LoadedCommand = ReactiveCommand.Create(TrySelectNextSegmentToTranslate);
            SequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            SequencerPlayerViewModel.SetAudio(await GetAudios());
            SequencerPlayerViewModel.SetupActivityService(ViewModelContextProvider, Disposables);

            SequencerActionViewModel = SequencerPlayerViewModel.CreateActionViewModel(
                required: Step.StepSettings.GetSetting(SettingType.RequireSegmentBTSectionListen),
                requirementId: Section.Id,
                provider: ViewModelContextProvider,
                disposables: Disposables);
            ActionViewModelBaseSourceList.Add(SequencerActionViewModel);

            SequencerPlayerViewModel
                .WhenAnyValue(player => player.State)
                .Where(state => state == SequencerState.Playing)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    SequencerActionViewModel.ActionState = ActionState.Optional;
                });
        }

        private void TrySelectNextSegmentToTranslate()
        {
            var orderedBackTranslations = _passage.CurrentDraftAudio.SegmentBackTranslationAudios
                    .OrderBy(x => x.TimeMarkers.StartMarkerTime)
                    .ToList();

            Func<SegmentBackTranslation, bool> segmentSelectCondidtion = IsSecondBackTranslation ? 
                (segment) => segment.RetellBackTranslationAudio is null || segment.RetellBackTranslationAudio.HasAudio is false : 
                (segment) => segment.HasAudio is false;

            var indexToSelect = Utilities.Utilities.GetSegmentIndexToSelect(
                backTranslations: orderedBackTranslations,
                previousBackTranslation: _selectedSegment,
                nextSegmentCondition: segmentSelectCondidtion);

            SequencerPlayerViewModel.TrySelectAudio(indexToSelect);
        }

        private async Task<PlayerAudioModel[]> GetAudios()
        {
            var audioList = new List<PlayerAudioModel>();

            await SetupSegmentBackTranslationsAsync();

            var orderedSegmentBackTranslations =
                _passage
                    .CurrentDraftAudio
                    .SegmentBackTranslationAudios
                    .OrderBy(x => x.TimeMarkers.StartMarkerTime).ToList();

            if (IsSecondBackTranslation is false)
            {
                var splitTimeMarkers = _passage.CurrentDraftAudio.SegmentBackTranslationAudios
                    .Where(x => x.TimeMarkers.StartMarkerTime > 0)
                    .Select(x => x.TimeMarkers.StartMarkerTime)
                    .ToList();

                var splitAudios = await ViewModelContextProvider.GetAudioEncodingService()
                    .SplitOpus(_passage.CurrentDraftAudio.Data, ViewModelContextProvider.GetAppDirectory().TempAudio,
                        48000, 1, splitTimeMarkers);

                var segmentNumber = 1;

                for (var i = 0; i < splitAudios.Count; i++)
                {
                    var segmentBackTranslation = orderedSegmentBackTranslations[i];

                    var segmentName = string.Format(AppResources.Segment, segmentNumber);

                    AudioOption option;
                    string endIcon = null;

                    if (segmentBackTranslation.HasAudio)
                    {
                        option = AudioOption.Completed;
                        endIcon = Icon.Checkmark.ToString();
                    }
                    else
                    {
                        option = AudioOption.Required;
                    }

                    var audio = PlayerAudioModel.Create(
                        path: splitAudios[i],
                        name: string.Format(AppResources.Segment, segmentNumber),
                        startIcon: Icon.SegmentNew.ToString(),
                        key: Guid.NewGuid(),
                        endIcon: endIcon,
                        option: option,
                        number: segmentNumber.ToString());

                    audioList.Add(audio);

                    _segmentAudioSegmentBackTranslationDictionary.Add(audio.Key, segmentBackTranslation.Id);
                    _segmentsDictionary.Add(segmentBackTranslation.Id, segmentName);
                    _segmentBackTranslationPassageDictionary.Add(segmentBackTranslation.Id, _passage.Id);
                    segmentNumber++;
                }
            }
            else // Step.Role = Roles.BackTranslate2
            {
                var segmentNumber = 1;

                foreach (var segmentBackTranslation in orderedSegmentBackTranslations)
                {
                    var segmentName = string.Format(AppResources.Segment, segmentNumber);

                    AudioOption option;
                    string endIcon = null;

                    if (segmentBackTranslation.RetellBackTranslationAudio != null &&
                        segmentBackTranslation.RetellBackTranslationAudio.HasAudio)
                    {
                        option = AudioOption.Completed;
                        endIcon = Icon.Checkmark.ToString();
                    }
                    else
                    {
                        option = AudioOption.Required;
                    }

                    var audio = segmentBackTranslation.CreatePlayerAudioModel(
                        audioType: ParentAudioType.SegmentBackTranslation,
                        name: segmentNumber.ToString(),
                        ViewModelContextProvider.GetTempAudioService(segmentBackTranslation).SaveTempAudio(),
                        endIcon: endIcon,
                        option: option,
                        number: segmentNumber.ToString(),
                        userId: ViewModelContextProvider.GetLoggedInUser().Id);

                    audioList.Add(audio);

                    _segmentAudioSegmentBackTranslationDictionary.Add(audio.Key, segmentBackTranslation.Id);
                    _segmentsDictionary.Add(segmentBackTranslation.Id, segmentName);
                    _segmentBackTranslationPassageDictionary.Add(segmentBackTranslation.Id, _passage.Id);
                    segmentNumber++;
                }
            }

            return audioList.ToArray();
        }

        private async Task SetupSegmentBackTranslationsAsync()
        {
            if (_passage.CurrentDraftAudio.SegmentBackTranslationAudios.Any())
            {
                return;
            }

            var breathPauseAnalyzer = ViewModelContextProvider.GetBreathPauseAnalyzer();
            var tempAudioService = ViewModelContextProvider.GetTempAudioService(_passage.CurrentDraftAudio);
            
            breathPauseAnalyzer.LoadAudioAndFindBreathPauses(tempAudioService);
            if (breathPauseAnalyzer.BreathPauseSegments != null)
            {
                var segmentRepository = ViewModelContextProvider.GetSegmentBackTranslationRepository();

                foreach (var bpSegment in breathPauseAnalyzer.BreathPauseSegments)
                {
                    var segment = new SegmentBackTranslation(
                        timeMarkers: bpSegment,
                        parentId: _passage.CurrentDraftAudio.Id,
                        toLanguageId: Guid.Empty,
                        fromLanguageId: Guid.Empty,
                        projectId: Section.ProjectId,
                        scopeId: Section.ScopeId);
                    
                    _passage.CurrentDraftAudio.SegmentBackTranslationAudios.Add(segment);

                    await segmentRepository.SaveAsync(segment);
                }
            }
        }

        private async Task<IRoutableViewModel> NavigateToSelectedBreathPauseSegmentAsync()
        {
            try
            {
                var audio = SequencerPlayerViewModel.GetCurrentAudio();

                if (audio is null)
                {
                    return default;
                }

                var segmentBackTranslationId = _segmentAudioSegmentBackTranslationDictionary[audio.Key];

                var passage = Section.Passages.Single(p =>
                    p.Id == _segmentBackTranslationPassageDictionary[segmentBackTranslationId]);

                var segment = passage.CurrentDraftAudio.SegmentBackTranslationAudios.SingleOrDefault(p =>
                    p.Id == segmentBackTranslationId);

                if (segment is null)
                {
                    return default;
                }

                var vm = await SegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(
                    Section, _passage, Stage, Step, segment, _segmentsDictionary[segment.Id],
                    ViewModelContextProvider);

                return await NavigateTo(vm);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        public override void Dispose()
        {
            SequencerPlayerViewModel?.Dispose();
            SequencerPlayerViewModel = null;
            SequencerActionViewModel?.Dispose();
            SequencerActionViewModel = null;

            _passage = null;

            base.Dispose();
        }
    }
}