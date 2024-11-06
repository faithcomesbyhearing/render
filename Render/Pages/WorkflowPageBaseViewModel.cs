using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Render.Components.ProceedButton;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages
{
    public class WorkflowPageBaseViewModel : PageViewModelBase
    {
        public SourceList<IActionViewModelBase> ActionViewModelBaseSourceList { get; private set; } = new ();

        private ReadOnlyObservableCollection<IActionViewModelBase> _actionViewModels;
        public ReadOnlyObservableCollection<IActionViewModelBase> ActionViewModels => _actionViewModels;
        public ProceedButtonViewModel ProceedButtonViewModel { get; protected set; }

        protected PassageNumber PassageNumber { get; }
        private Guid NonDraftTranslationId { get; }
        protected Step Step { get; set; }
        public Section Section { get; set; }
        protected Stage Stage;

        /// <summary>
        /// This method can be overridden on workflow pages in order to set the checkmark icon for the proceed button
        /// when necessary.
        /// </summary>
        protected virtual void SetProceedButtonIcon()
        {
        }

        public WorkflowPageBaseViewModel(
            string urlPathSegment,
            IViewModelContextProvider viewModelContextProvider,
            string pageName,
            Section section, Stage stage, Step step = null,
            PassageNumber passageNumber = null,
            Guid nonDraftTranslationId = default,
            bool isSegmentSelect = false,
            string secondPageName = "")
            : base(urlPathSegment,
                viewModelContextProvider,
                pageName,
                new List<IMenuActionViewModel>(),
                section?.Number ?? 0,
                section?.Title.Audio,
                section: section,
                passageNumber: passageNumber,
                stage: stage,
                step: step,
                isSegmentSelect: isSegmentSelect,
                secondPageName: secondPageName)
        {
            Step = step;
            PassageNumber = passageNumber;
            Section = section;
            Stage = stage;
            NonDraftTranslationId = nonDraftTranslationId;
            SetUserSession();
            ProceedButtonViewModel = new ProceedButtonViewModel(viewModelContextProvider);
            var changeList = ActionViewModelBaseSourceList
                .Connect()
                .Publish();
            Disposables.Add(changeList
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _actionViewModels)
                .Subscribe());
            
            Disposables.Add(changeList
                .WhenPropertyChanged(x => x.ActionState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x.Sender)
                .Subscribe(vm =>
                {
                    ProcessStateStatusChange();
                    if (ActionViewModelBaseSourceList.Items.Any(action => action.ActionState == ActionState.Required))
                    {
                        ProceedButtonViewModel.ProceedActive = false;
                        return;
                    }

                    ProceedButtonViewModel.ProceedActive = true;
                }));
            Disposables.Add(changeList.Connect());
            Disposables.Add(TitleBarViewModel.TitleBarMenuViewModel.NavigationItems.Observable
                .MergeMany(item => item.Command.IsExecuting)
                .Subscribe(SetLoadingScreen));

            Disposables.Add(TitleBarViewModel.NavigationItems.Observable
                .MergeMany(item => item.IsExecuting)
                .Subscribe(isExecuting => IsLoading = isExecuting));
        }

        protected virtual void ProcessStateStatusChange()
        {
        }

        protected override void OnNavigatingBack()
        {
            base.OnNavigatingBack();
            SetUserSession();
        }

        public void SetUserSession()
        {
            if (Step?.Id != default && Section?.Id != default)
            {
                SessionStateService.StartPage(Section.Id, UrlPathSegment, PassageNumber, NonDraftTranslationId, Step);
            }
        }
        
        protected async Task<IRoutableViewModel> NavigateToHomeOnMainStackAsync()
        {
            try
            {
                var grandCentralStation = ViewModelContextProvider.GetGrandCentralStation();
                var loggedInUserId = GetLoggedInUserId();
                await Task.Run(async () =>
                {
                    await SessionStateService.FinishSessionAsync();
                    await grandCentralStation.FindWorkForUser(GetProjectId(), loggedInUserId);
                });
                return await FinishCurrentStackAndNavigateHome();
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }
        
        public async void OpenNoMicWarningModal()
        {
            var modalManager = ViewModelContextProvider.GetModalService();
            await modalManager.ShowInfoModal(
                icon: Icon.MicrophoneWarning,
                title: AppResources.MicrophoneAccessTitle,
                message: AppResources.MicrophoneAccessMessage);

            LogInfo("Microphone is not connected");
        }
        
        public override void Dispose()
        {
            Section = null;
            Step = null;
            Stage = null;
            ProceedButtonViewModel?.Dispose();

            ActionViewModelBaseSourceList?.Clear();
            ActionViewModelBaseSourceList?.Dispose();
            ActionViewModelBaseSourceList = null;
            _actionViewModels = null;

            base.Dispose();
        }
    }
}