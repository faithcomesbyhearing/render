using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Pages.Translator.PassageListen;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;
using Render.Services.AudioServices;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Render.Pages.Translator.SectionListen
{
    public class SectionListenViewModel : WorkflowPageBaseViewModel
    {
        public SourceList<IBarPlayerViewModel> ReferenceSourceList { get; } = new();
        public ReadOnlyObservableCollection<IBarPlayerViewModel> ReferenceList => _referenceList;
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _referenceList;

        public SourceList<IBarPlayerViewModel> SupplementaryMaterialSourceList { get; } = new();
        public ReadOnlyObservableCollection<IBarPlayerViewModel> SupplementaryMaterialList => _supplementaryMaterialList;
        private readonly ReadOnlyObservableCollection<IBarPlayerViewModel> _supplementaryMaterialList;

        private SectionListenViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            string pageName,
            string secondPageName,
            Step step,
            Stage stage)
            : base(
                urlPathSegment: "SectionListen",
                viewModelContextProvider: viewModelContextProvider,
                pageName: pageName,
                section: section,
                stage: stage,
                step: step,
                secondPageName: secondPageName)
        {
            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            
            ProceedButtonViewModel.SetCommand(NavigateForwardAsync);

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

            Disposables.Add(section.WhenAnyValue(x => x.HasReferenceAudio)
                .Subscribe(PopulateReferences));
            Disposables.Add(ProceedButtonViewModel.NavigateToPageCommand.IsExecuting
                .Subscribe(isExecuting => { IsLoading = isExecuting; }));
        }

        private void PopulateReferences(bool hasReferenceAudio)
        {
            if (!hasReferenceAudio) return;
            var referenceCount = 0;
            foreach (var referenceAudio in Section.References)
            {
                var isRequired = referenceAudio.Reference.Primary &&
                                 Step.StepSettings.GetSetting(SettingType.RequireSectionListen);
                var barPlayer = ViewModelContextProvider.GetBarPlayerViewModel(referenceAudio,
                    isRequired ? ActionState.Required : ActionState.Optional,
                    referenceAudio.Reference.Name, referenceCount++,
                    passageMarkers: referenceAudio.PassageReferences.Select(x => x.TimeMarkers).ToList());
                ReferenceSourceList.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }

            PopulateSupplementalMaterials();
        }

        private void PopulateSupplementalMaterials()
        {
            var supplementaryCount = 0;
            foreach (var referenceAudio in Section.SupplementaryMaterials)
            {
                var isRequired = Step.StepSettings.GetSetting(SettingType.RequireSectionListen);
                if (referenceAudio.Audio == null)
                {
                    continue;
                }

                var barPlayer = ViewModelContextProvider.GetBarPlayerViewModel(referenceAudio.Audio,
                    isRequired ? ActionState.Required : ActionState.Optional, referenceAudio.Name,
                    supplementaryCount++);
                SupplementaryMaterialSourceList.Add(barPlayer);
                ActionViewModelBaseSourceList.Add(barPlayer);
            }
        }

        public static SectionListenViewModel Create(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            Step step,
            Stage stage)
        {
            var title = GetStepName(step);
            var secondTitle = AppResources.SectionListenScreenTitle;
            var sectionListenViewModel = new SectionListenViewModel(viewModelContextProvider, section, title, secondTitle,
                step, stage);
            return sectionListenViewModel;
        }

        private async Task<IRoutableViewModel> NavigateForwardAsync()
        {
            ViewModelBase vm;
            if (Step.StepSettings.GetSetting(SettingType.DoPassageListen))
            {
                vm = await PassageListenViewModel.CreateAsync(ViewModelContextProvider, Section, Step, Section
                    .Passages.First(), Stage);
            }
            else
            {
                var draftingStage = ViewModelContextProvider.GetWorkflowService().ProjectWorkflow.DraftingStage;
                vm = await DraftingViewModel.CreateAsync(Section, Section.Passages.First(), Step,
                    ViewModelContextProvider, draftingStage);
            }

            return await NavigateTo(vm);
        }

        public override void Dispose()
        {
            foreach (var reference in ReferenceList)
            {
                reference.Dispose();
            }

            foreach (var reference in SupplementaryMaterialList)
            {
                reference.Dispose();
            }

            ReferenceSourceList?.Dispose();
            SupplementaryMaterialSourceList?.Dispose();
            base.Dispose();
        }
    }
}