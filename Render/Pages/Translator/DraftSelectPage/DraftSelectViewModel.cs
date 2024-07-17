using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Components.DraftSelection;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Project;
using Render.Models.Scope;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.CommunityTester.CommunityCheckRevise;
using Render.Pages.Revise.NoteListen;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.PassageListen;
using Render.Resources;
using Render.Resources.Localization;
using Draft = Render.Models.Sections.Draft;

namespace Render.Pages.Translator.DraftSelectPage
{
    public class DraftSelectViewModel : WorkflowPageBaseViewModel
    {
        private SourceList<IDraftSelectionViewModel> DraftSourceList { get; set; } = new();
        public ReadOnlyObservableCollection<IDraftSelectionViewModel> Drafts => _drafts;
        private readonly ReadOnlyObservableCollection<IDraftSelectionViewModel> _drafts;

        private List<DraftViewModel> _draftViewModels;
        private Passage Passage { get; set; }

        public DraftSelectViewModel(Section section, IEnumerable<DraftViewModel> drafts,
            IViewModelContextProvider viewModelContextProvider, Passage passage, Step step, Stage stage)
            : base("DraftSelect", viewModelContextProvider,
                GetStepName(viewModelContextProvider, step.RenderStepType, stage.Id), section, stage,
                step, passage.PassageNumber, secondPageName: AppResources.DraftSelect)
        {
            DisposeOnNavigationCleared = true;
            TitleBarViewModel.DisposeOnNavigationCleared = true;

            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;

            var draftViewModels = drafts.ToList();
            _draftViewModels = draftViewModels.ToList();
            Passage = passage;
            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting =>
                {
                    IsLoading = isExecuting;
                }));

            var changeList = DraftSourceList
                .Connect()
                .Publish();
            Disposables.Add(changeList
                .Bind(out _drafts)
                .Subscribe());
            Disposables.Add(changeList
                .WhenPropertyChanged(s => s.DraftSelectionState, false)
                .Select(c => c.Sender)
                .Subscribe(vm =>
                {
                    if (vm.DraftSelectionState == DraftSelectionState.Selected)
                    {
                        foreach (var draft in Drafts)
                        {
                            if (draft != vm)
                            {
                                draft.DraftSelectionState = DraftSelectionState.Unselected;
                            }
                        }
                    }

                    foreach (var action in ActionViewModels)
                    {
                        action.ActionState = ActionState.Optional;
                    }
                }));

            Disposables.Add(changeList.Connect());

            //populate the draft list
            foreach (var draft in draftViewModels)
            {
                var miniWaveformPlayer = ViewModelContextProvider.GetMiniWaveformPlayerViewModel(draft.Audio,
                    ActionState.Optional, draft.Title);
                DraftSourceList.Add(ViewModelContextProvider.GetDraftSelectionViewModel(
                    miniWaveformPlayer, ActionState.Required));
            }
            ActionViewModelBaseSourceList.AddRange(_drafts);
            SetProceedButtonIcon();
        }

        protected sealed override void SetProceedButtonIcon()
        {
            if (Step.RenderStepType == RenderStepTypes.Draft)
            {
                var nextPassage = Section.GetNextPassage(Passage);
                if (!Step.StepSettings.GetSetting(SettingType.DoPassageReview) &&
                    nextPassage == null && 
                    !Step.StepSettings.GetSetting(SettingType.DoSectionReview))
                {
                    ProceedButtonViewModel.IsCheckMarkIcon = true;
                }
            }
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            await SaveSelectedDraftAsync(Passage);
            //Update Project Statistics
            var statisticsPersistence = ViewModelContextProvider.GetPersistence<RenderProjectStatistics>();
            var projectStatistics = (await statisticsPersistence.QueryOnFieldAsync("ProjectId", Section.ProjectId.ToString(), 1, false)).FirstOrDefault();
            if (projectStatistics != null && projectStatistics.FirstSectionDraftedDate.Year == 1)
            {
                projectStatistics.SetFirstSectionDraftedDate(DateTimeOffset.Now);
                await statisticsPersistence.UpsertAsync(projectStatistics.Id, projectStatistics);
            }

            //Update Scope class with first section drafted date
            var scopePersistence = ViewModelContextProvider.GetPersistence<Scope>();
            var scope = await scopePersistence.GetAsync(Section.ScopeId);
            if (scope != null && scope.FirstSectionDraftedDate.Year == 1)
            {
                scope.SetFirstSectionDraftedDate(DateTimeOffset.Now);
                await scopePersistence.UpsertAsync(scope.Id, scope);
            }

            ViewModelBase vm;
            if (Step.RenderStepType == RenderStepTypes.Draft)
            {
                var nextPassage = Section.GetNextPassage(Passage);
                if (Step.StepSettings.GetSetting(SettingType.DoPassageReview))
                {
                    vm = await PassageReview.PassageReview.GetPassageReviewViewModelAsync(
                        ViewModelContextProvider, Section, Passage, Step);
                }
                else if (nextPassage != null && !Section.Passages.All(x => x.HasAudio))
                {
                    if (Step.StepSettings.GetSetting(SettingType.DoPassageListen))
                    {
                        vm = await PassageListenViewModel.CreateAsync(ViewModelContextProvider, Section, Step,
                            nextPassage, Stage);
                    }
                    else
                    {
                        vm = await DraftingViewModel.CreateAsync(Section, nextPassage, Step,
                            ViewModelContextProvider, Stage);
                    }
                }
                else
                {
                    if (Step.StepSettings.GetSetting(SettingType.DoSectionReview))
                    {
                        vm = await SectionReview.SectionReview.GetSectionReviewViewModelAsync(Section,
                            ViewModelContextProvider, Step);
                        
                        return await NavigateToAndReset(vm);
                    }
                    else
                    {
                        await ViewModelContextProvider.GetGrandCentralStation().AdvanceSectionAsync(Section, Step);

                        await RemoveTempAudioFromLocalDatabase();

                        return await NavigateToHomeOnMainStackAsync();
                    }
                }
            }
            //Else we're revising
            else
            {
                //Since it is going back to consultant check we reset the checked by field
                //so that it will properly re-enter the revise loop if necessary
                if (Step.RenderStepType == RenderStepTypes.ConsultantRevise)
                {
                    Section.SetCheckedBy(Guid.Empty);
                }
                if (Step.RenderStepType == RenderStepTypes.CommunityRevise)
                {
                    if (Step.StepSettings.GetSetting(SettingType.DoPassageReview))
                    {
                        SessionStateService.AddRequirementCompletion(Passage.Id);
                        vm = await PassageReview.PassageReview.GetPassageReviewViewModelAsync(
                            ViewModelContextProvider, Section, Passage, Step);
                    }
                    else
                    {
                        SessionStateService.AddRequirementCompletion(Passage.Id);
                        vm = await Task.Run(async () => await CommunityRevisePageViewModel.CreateAsync(ViewModelContextProvider, Section, Step, Stage));
                        return await NavigateToAndReset(vm);
                    }
                }
                else if (Step.StepSettings.GetSetting(SettingType.DoPassageReview))
                {
                    vm = await PassageReview.PassageReview.GetPassageReviewViewModelAsync(
                        ViewModelContextProvider, Section, Passage, Step);
                }
                else
                {
                    vm = await NoteListen.GetNoteListenViewModelAsync(ViewModelContextProvider, Section, Step);
                }
            }

            await RemoveTempAudioFromLocalDatabase();

            return await NavigateToAndReset(vm);
        }

        private async Task RemoveTempAudioFromLocalDatabase()
        {
            var audioIds = _draftViewModels.Select(d => d.Audio.Id).ToList();
            await ViewModelContextProvider.GetSessionStateService().DeleteAllTemporaryAudioAsync(audioIds);
        }

        private async Task SaveSelectedDraftAsync(Passage passage)
        {
            var selectedDraft = Drafts.SingleOrDefault(x =>
                x.DraftSelectionState == DraftSelectionState.Selected);
            var draftViewModel = _draftViewModels.SingleOrDefault(x =>
                selectedDraft != null && x.Title == selectedDraft.MiniWaveformPlayerViewModel.AudioTitle);
            if (draftViewModel is { IsPreviousDraft: false })
            {
                var draft = new Draft(Section.ScopeId, Section.ProjectId, passage.Id);
                draft.SetAudio(draftViewModel.Audio.Data);
                draft.SetRevision(passage.CurrentDraftAudio is null ? 0 : passage.CurrentDraftAudio.Revision + 1);

                if (passage.CurrentDraftAudio != null && passage.CurrentDraftAudio.CreatedById != default)
                {
                    draft.SetCreatedBy(passage.CurrentDraftAudio.CreatedById, passage.CurrentDraftAudio.CreatedByName);
                }
                else
                {
                    var loggedInUser = ViewModelContextProvider.GetLoggedInUser();
                    draft.SetCreatedBy(loggedInUser.Id, loggedInUser.Username);
                }

                draft.SavedDuration = draftViewModel.Audio.SavedDuration;

                //TODO PBI 4986: Save a temporary snapshot here of the previous draft
                passage.ChangeCurrentDraftAudio(draft);
                await ViewModelContextProvider.GetSectionRepository().SaveSectionAndDraftAsync(Section, draft);
            }
        }

        public override void Dispose()
        {
            Passage = null;
            _draftViewModels = null;

            DraftSourceList.DisposeSourceList();

            base.Dispose();
        }
    }
}