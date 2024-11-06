using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Audio;
using Render.Resources.Localization;
using Render.Sequencer.Contracts.Interfaces;
using Render.Sequencer.Contracts.Models;
using Render.Sequencer.Core.Utils.Extensions;
using Render.Utilities;

namespace Render.Pages.BackTranslator.SegmentBackTranslate.SegmentEditing
{
    public class TabletSegmentCombinePageViewModel : WorkflowPageBaseViewModel
    {
        private Passage _passage;
        private SegmentBackTranslation _selectedSegmentBackTranslation;
        private IAudioRepository<SegmentBackTranslation> _segmentRepository;

        [Reactive] public ISequencerCombiningPlayerViewModel CombiningSequencerPlayerViewModel { get; private set; }

        public static Task<TabletSegmentCombinePageViewModel> CreateAsync(
            Step step,
            Section section,
            Passage passage,
            SegmentBackTranslation selectedSegmentBackTranslation,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage)
        {
            return Task.Run(async () =>
            {
                var segmentCombinePageModel = new TabletSegmentCombinePageViewModel(step: step,
                    section: section,
                    selectedSegmentBackTranslation: selectedSegmentBackTranslation,
                    viewModelContextProvider: viewModelContextProvider,
                    stage: stage,
                    passage: passage);

                await segmentCombinePageModel.SetupSequencer();

                return segmentCombinePageModel;
            });
        }

        protected TabletSegmentCombinePageViewModel(
            Step step,
            Section section,
            SegmentBackTranslation selectedSegmentBackTranslation,
            IViewModelContextProvider viewModelContextProvider,
            Stage stage,
            Passage passage) : base(
                urlPathSegment: "TabletSegmentCombine",
                viewModelContextProvider: viewModelContextProvider,
                pageName: GetStepName(step),
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage?.PassageNumber,
                secondPageName: AppResources.SegmentCombine)
        {
            TitleBarViewModel.PageGlyph = ((FontImageSource)ResourceExtensions.GetResourceValue("SegmentBackTranslateWhite"))?.Glyph;

            _passage = passage;
            _selectedSegmentBackTranslation = selectedSegmentBackTranslation;
            _segmentRepository = ViewModelContextProvider.GetSegmentBackTranslationRepository();

            ProceedButtonViewModel.SetCommand(NavigateToSelectedBreathPauseSegmentAsync);

            Disposables.Add(ProceedButtonViewModel
                .NavigateToPageCommand
                .IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        private async Task SetupSequencer()
        {
            CombiningSequencerPlayerViewModel = ViewModelContextProvider
                .GetSequencerFactory()
                .CreateCombiningPlayer(ViewModelContextProvider.GetAudioPlayer);

            CombiningSequencerPlayerViewModel.IsRightToLeftDirection = FlowDirection is FlowDirection.RightToLeft;

            CombiningSequencerPlayerViewModel.TryUnlockAudioCommand = ReactiveCommand.CreateFromTask<AudioModel, bool>(TryUnlockAudio);
            CombiningSequencerPlayerViewModel.SetAudio(await GetAudios());
        }

        private async Task<bool> TryUnlockAudio(AudioModel model)
        {
            var modalManager = ViewModelContextProvider.GetModalService();
            var result = await modalManager.ConfirmationModal(
                icon: Resources.Icon.UnlockSegment,
                title: AppResources.CombineWarningTitle,
                message: AppResources.CombineWarningMessage,
                cancelText: AppResources.Cancel,
                confirmText: AppResources.Proceed);

            return result is Kernel.WrappersAndExtensions.DialogResult.Ok;
        }

        private async Task<CombinableAudioModel[]> GetAudios()
        {
            var orderedSegmentBackTranslations = _passage.CurrentDraftAudio.SegmentBackTranslationAudios
                .OrderBy(audios => audios.TimeMarkers.StartMarkerTime)
                .ToList();

            var splitTimeMarkers = _passage.CurrentDraftAudio.SegmentBackTranslationAudios
                .Where(audios => audios.TimeMarkers.StartMarkerTime > 0)
                .Select(audios => audios.TimeMarkers.StartMarkerTime)
                .ToList();

            var splitAudiosPaths = await ViewModelContextProvider
                .GetAudioEncodingService()
                .SplitOpus(
                    opusData: _passage.CurrentDraftAudio.Data,
                    tempAudioDirectoryPath: ViewModelContextProvider.GetAppDirectory().TempAudio,
                    sampleRate: 48000,
                    channelCount: 1,
                    timeMarkersInMilliseconds: splitTimeMarkers);

            var segmentName = AppResources.SegmentLabel;
            var audios = new CombinableAudioModel[splitAudiosPaths.Count];
            for (var i = 0; i < splitAudiosPaths.Count; i++)
            {
                var segmentBackTranslation = orderedSegmentBackTranslations[i];
                var isBaseAudio = segmentBackTranslation.Id == _selectedSegmentBackTranslation.Id;
                var isLocked = segmentBackTranslation.HasAudio && isBaseAudio is false;

                audios[i] = CombinableAudioModel.Create(
                    key: segmentBackTranslation.Id,
                    startIcon: isBaseAudio ? Resources.Icon.PrimaryGeneral.ToString() : Resources.Icon.SegmentNew.ToString(),
                    path: splitAudiosPaths[i],
                    name: segmentName,
                    isBase: isBaseAudio,
                    isLocked: isLocked,
                    number: (i + 1).ToString());
            }

            return audios;
        }

        protected async Task<IRoutableViewModel> NavigateToSelectedBreathPauseSegmentAsync()
        {
            var combinedResult = CombiningSequencerPlayerViewModel.GetCombinedResult();
            if (combinedResult.HasNewCombinedSegments)
            {
                var newCombinedBackTranslation = new SegmentBackTranslation(
                    timeMarkers: new TimeMarkers(combinedResult.StartTime.ToMilliseconds(), combinedResult.EndTime.ToMilliseconds()),
                    parentId: _selectedSegmentBackTranslation.ParentId,
                    toLanguageId: _selectedSegmentBackTranslation.ToLanguageId,
                    fromLanguageId: _selectedSegmentBackTranslation.FromLanguageId,
                    projectId: _selectedSegmentBackTranslation.ProjectId,
                    scopeId: _selectedSegmentBackTranslation.ScopeId);

                await _segmentRepository.SaveAsync(newCombinedBackTranslation);

                var originBackTranslations = _passage.CurrentDraftAudio.SegmentBackTranslationAudios;
                var backTranslationsToDelete = originBackTranslations.IntersectBy(
                    second: combinedResult.CombinedAudiosIds,
                    keySelector: (backTranslation) => backTranslation.Id);

                foreach (var backTranslationToDelete in backTranslationsToDelete)
                {
                    await _segmentRepository.DeleteAudioByIdAsync(backTranslationToDelete.Id);
                }

                var updatedBackTranslations = originBackTranslations
                    .Except(backTranslationsToDelete)
                    .Append(newCombinedBackTranslation)
                    .OrderBy(backTranslation => backTranslation.TimeMarkers.StartMarkerTime);

                _passage.CurrentDraftAudio.SegmentBackTranslationAudios = updatedBackTranslations.ToList();
                _selectedSegmentBackTranslation = newCombinedBackTranslation;
            }

            var selectedBackTranslationIndex = _passage.CurrentDraftAudio.SegmentBackTranslationAudios.IndexOf(_selectedSegmentBackTranslation);
            var segmentTranslatePageModel = await SegmentBackTranslateResolver.GetSegmentTranslatePageViewModel(section: Section,
                passage: _passage,
                stage: Stage,
                step: Step,
                segmentBackTranslation: _selectedSegmentBackTranslation,
                segmentName: string.Format(AppResources.Segment, selectedBackTranslationIndex + 1),
                viewModelContextProvider: ViewModelContextProvider,
                overrideBackCommand: true);

            return await NavigateToAndReset(segmentTranslatePageModel);
        }

        public override void Dispose()
        {
            _passage = null;
            _selectedSegmentBackTranslation = null;

            CombiningSequencerPlayerViewModel?.Dispose();
            CombiningSequencerPlayerViewModel = null;

            base.Dispose();
        }
    }
}