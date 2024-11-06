using DynamicData;
using ReactiveUI;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Resources;
using Render.Resources.Localization;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Render.Pages.Translator.PassageListen
{
    public class PassageListenViewModel : WorkflowPageBaseViewModel
    {
        public SourceList<IBarPlayerViewModel> ReferenceSourceList { get; } = new();
        public ReadOnlyObservableCollection<IBarPlayerViewModel> ReferenceList => _referenceList;
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _referenceList;

        public SourceList<IBarPlayerViewModel> SupplementaryMaterialSourceList { get; } = new();

        public ReadOnlyObservableCollection<IBarPlayerViewModel> SupplementaryMaterialList => _supplementaryMaterialList;

        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _supplementaryMaterialList;

        public Passage Passage { get; set; }

        private PassageListenViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Passage passage,
            string pageName,
            string secondPageName,
            Stage stage)
            : base(
                urlPathSegment: "PassageListen",
                viewModelContextProvider: viewModelContextProvider,
                pageName: pageName,
                section: section,
                stage: stage,
                step: step,
                passageNumber: passage.PassageNumber,
                secondPageName: secondPageName)
        {
            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            //Reference bar players 
            var referenceChangeList = ReferenceSourceList.Connect().Publish();
            Disposables.Add(referenceChangeList
                .Bind(out _referenceList)
                .Subscribe());
            Disposables.Add(referenceChangeList.Connect());

            //Reference supplementary bar players 
            var supplementaryChangeList = SupplementaryMaterialSourceList.Connect().Publish();
            Disposables.Add(supplementaryChangeList
                .Bind(out _supplementaryMaterialList)
                .Subscribe());
            Disposables.Add(supplementaryChangeList.Connect());
            ProceedButtonViewModel.SetCommand(NavigateToDraftingAsync);

            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
            Passage = passage;
        }

        private void PopulateReferences(Passage passage)
        {
            var referenceCount = 0;
            var passageNumber = passage.PassageNumber.Number;
            foreach (var referenceAudio in Section.References.Where(x => !x.LockedReferenceByPassageNumbersList.Contains(passageNumber)))
            {
                var timeMarkers = referenceAudio.PassageReferences.FirstOrDefault(x =>
                        x.PassageNumber.Equals(passage.PassageNumber))
                    ?.TimeMarkers;
                var isRequired = referenceAudio.Reference.Primary && Step.StepSettings.GetSetting(SettingType.RequirePassageListen);
                var barPlayer = ViewModelContextProvider.GetBarPlayerViewModel(referenceAudio,
                    isRequired ? ActionState.Required : ActionState.Optional,
                    referenceAudio.Reference.Name,
                    referenceCount++,
                    timeMarkers);
                ReferenceSourceList.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }
        }

        private void PopulateSupplementalMaterials(Passage passage)
        {
            var supplementaryCount = 0;
            foreach (var media in passage.SupplementaryMaterials)
            {
                var isRequired = Step.StepSettings.GetSetting(SettingType.RequirePassageListen);
                var barPlayer = new BarPlayerViewModel(media.Audio, ViewModelContextProvider,
                    isRequired ? ActionState.Required : ActionState.Optional, media.Name,
                    supplementaryCount++);
                SupplementaryMaterialSourceList.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }
        }

        public static async Task<PassageListenViewModel> CreateAsync(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Passage passage,
            Stage stage)
        {
            var title = GetStepName(step);
            var secondTitle = AppResources.PassageListenScreenTitle;

            var passageListenViewModel = new PassageListenViewModel(viewModelContextProvider, section, step,
                passage, title, secondTitle, stage);
            var sectionRepository = viewModelContextProvider.GetSectionRepository();
            await sectionRepository.GetPassageSupplementaryMaterials(section, passage);
            passageListenViewModel.PopulateReferences(passage);
            passageListenViewModel.PopulateSupplementalMaterials(passage);

            return passageListenViewModel;
        }

        private async Task<IRoutableViewModel> NavigateToDraftingAsync()
        {
            var draftingViewModel = await DraftingViewModel.CreateAsync(Section, Passage, Step, ViewModelContextProvider, Stage);
            return await NavigateToAndReset(draftingViewModel);
        }

        public override void Dispose()
        {
            foreach (var reference in ReferenceList)
            {
                reference.Dispose();
            }

            ReferenceSourceList?.Dispose();
            foreach (var reference in SupplementaryMaterialList)
            {
                reference.Dispose();
            }

            SupplementaryMaterialSourceList?.Dispose();
            Passage = null;

            base.Dispose();
        }
    }
}