using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Render.Components.DivisionPlayer;
using Render.Extensions;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Pages.Translator.DraftingPage;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.Translator.DividePassagePage
{
    public class DividePassageViewModel : WorkflowPageBaseViewModel
    {
        private bool _isChanged;

        public IObservableCollection<IDivisionPlayerViewModel> DivisionPlayers { get; private set; } =
            new ObservableCollectionExtended<IDivisionPlayerViewModel>();

        public static DividePassageViewModel Create(IViewModelContextProvider viewModelContextProvider,
            Section section, PassageNumber passageNumber, Step step, Stage stage)
        {
            var title = AppResources.Draft;
            var secondTitle = AppResources.PassageDivide;
            var dividePassageViewModel = new DividePassageViewModel(
                viewModelContextProvider,
                section,
                passageNumber,
                title,
                secondTitle,
                step,
                stage);
            
            return dividePassageViewModel;
        }

        private DividePassageViewModel(
            IViewModelContextProvider viewModelContextProvider,
            Section section,
            PassageNumber passageNumber,
            string pageName,
            string secondPageName,
            Step step,
            Stage stage) :
            base(
                viewModelContextProvider: viewModelContextProvider,
                section: section,
                passageNumber: passageNumber,
                pageName: pageName,
                urlPathSegment: "TabletDividePassage",
                step: step,
                stage: stage,
                secondPageName: secondPageName)
        {
            TitleBarViewModel.PageGlyph = ResourceExtensions.GetResourceValue(Icon.Record.ToString()) as string;
            
            TitleBarViewModel.SetNavigationCondition(AllowNavigation);
            
            ProceedButtonViewModel.SetCommand(NavigateToDraftingAsync);
            
            PopulateDivisionPlayers(passageNumber);
        }
        
        private async Task<bool> AllowNavigation()
        {
            if (!_isChanged)
            {
                return true;
            }

            var modalManager = ViewModelContextProvider.GetModalService();
            var result = await modalManager.ConfirmationModal(Icon.TrashCanAudio, AppResources.WorkWillBeLost,
                AppResources.DividePassageMessage,
                AppResources.Cancel, AppResources.DiscardWork);
            
            return result is DialogResult.Ok;
        }

        private void CreatePlayerViewModel(
            SectionReferenceAudio reference,
            List<PassageReference> passageReferences,
            TimeMarkers fullPassageTimeMarker,
            PassageNumber passageNumber)
        {
            var divisionPlayer = ViewModelContextProvider.GetDivisionPlayerViewModel(
                reference, passageReferences, fullPassageTimeMarker, passageNumber);

            DivisionPlayers.Add(divisionPlayer);
            
            ActionViewModelBaseSourceList.Add(divisionPlayer);

            Disposables.Add(divisionPlayer.WhenAnyValue(p => p.DivisionsCount, p => p.IsLocked)
                .Subscribe(_ => SynchronizePlayers()));
        }
        
        private async Task<IRoutableViewModel> NavigateToDraftingAsync()
        {
            var stage = ViewModelContextProvider.GetGrandCentralStation().ProjectWorkflow.DraftingStage;
            var passage = Section.Passages.FirstOrDefault(x => x.PassageNumber.Equals(PassageNumber));
            
            if (_isChanged)
            {
                DivisionPlayers.ForEach(p => p.ApplyChanges());
                Section.References = DivisionPlayers.Select(p => p.ReferenceAudio).ToList();

                var dividedPassages = Section.References
                    .SelectMany(x => x.PassageReferences)
                    .Where(x => x.PassageNumber.DivisionNumber > 0 && x.PassageNumber.Number == PassageNumber.Number)
                    .Select(x => x.PassageNumber)
                    .Distinct()
                    .Select(pn => new Passage(pn))
                    .ToList();

                dividedPassages.ForEach(devidedPassage =>
                {
					var source = Section.Passages.Find(sourcePassage =>
						sourcePassage.PassageNumber.Number == devidedPassage.PassageNumber.Number
                        && (sourcePassage.ScriptureReferences?.Count ?? 0) > 0);
                    if (source is not null)
                    {
                        devidedPassage.SetScriptureReferences(source.ScriptureReferences);
                    }
				});
				// Passages may not be divided if the user has not made any divisions and has only locked some passages 
				if (dividedPassages.Count != 0)
                {
                    Section.Passages.RemoveAll(p => p.PassageNumber.Number == PassageNumber.Number);
                    Section.Passages.AddRange(dividedPassages);
                    Section.SetPassages(Section.Passages.OrderBy(x => x.PassageNumber).ToList());

                    SessionStateService.ActiveSession.ResetAudios();
                }

                passage = Section.Passages.FirstOrDefault(x => x.PassageNumber.Equals(PassageNumber)) ??
                          Section.Passages.First(x => x.PassageNumber.Number == PassageNumber.Number);

                SessionStateService.ActiveSession.SetPassageNumber(passage.PassageNumber);

                var sectionRepository = ViewModelContextProvider.GetSectionRepository();
                await sectionRepository.SaveSectionWithNewDivisionsAsync(Section);
            }

            var vm = await Task.Run(async () => await DraftingViewModel.CreateAsync(Section, passage, Step,
                ViewModelContextProvider, stage));
            
            return await NavigateToAndReset(vm);
        }

        private void PopulateDivisionPlayers(PassageNumber passageNumber)
        {
            foreach (var referenceAudio in Section.References)
            {
                var passageReferences = referenceAudio.PassageReferences
                    .Where(r => r.PassageNumber.Equals(passageNumber)).ToList();

                var fullPassageTimeMarker = new TimeMarkers(
                    passageReferences.First().TimeMarkers.StartMarkerTime,
                    passageReferences.Last().TimeMarkers.EndMarkerTime);

                CreatePlayerViewModel(referenceAudio, passageReferences, fullPassageTimeMarker, passageNumber);
            }
        }
        
        private void SynchronizePlayers()
        {
            var divisions = DivisionPlayers
                .Where(player => !player.IsLocked)
                .Select(player => player.DivisionsCount)
                .Distinct()
                .ToList();
            
            DivisionPlayers.First().ActionState = divisions.Count > 1 || DivisionPlayers.All(player => player.IsLocked)
                ? ActionState.Required 
                : ActionState.Optional;

            _isChanged = divisions.Any(d => d != 0) || DivisionPlayers.Any(player => player.IsLocked);
        }

        public override void Dispose()
        {
            DivisionPlayers.DisposeCollection();
            DivisionPlayers = null;
            
            base.Dispose();
        }
    }
}