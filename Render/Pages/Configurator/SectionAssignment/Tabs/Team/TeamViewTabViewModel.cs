using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Specialized;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Extensions;
using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment.Utils;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;
using Render.Pages.Configurator.SectionAssignment.Manager;

namespace Render.Pages.Configurator.SectionAssignment.Tabs.Team
{
    public class TeamViewTabViewModel : ViewModelBase
    {
        private SectionIndexChangeBucket _indexChangeBucket;

        public SectionCollectionsManager Manager { get; private set; }

        [Reactive]
        public TeamCardViewModel SelectedTeam { get; private set; }

        [Reactive]
        public ReactiveCommand<Unit, Unit> SectionIndexChangedCommand { get; private set; }

        [Reactive]
        public ReactiveCommand<SectionCardViewModel, Unit> AssignSectionCommand { get; private set; }

        public TeamViewTabViewModel(
            IViewModelContextProvider viewModelContextProvider,
            SectionCollectionsManager manager,
            Guid? displayTeamId)
            : base(nameof(TeamViewTabViewModel), viewModelContextProvider)
        {
            Manager = manager;

            SectionIndexChangedCommand = ReactiveCommand.Create(SectionIndexChanged);
            AssignSectionCommand = ReactiveCommand.Create<SectionCardViewModel>(AssignSection);

            SelectInitialTeam(displayTeamId);
            AddSubscriptions();
        }

        private void AddSubscriptions()
        {
            Disposables.Add(Manager.TeamCards
                .AsObservableChangeSet()
                .WhenPropertyChanged(team => team.Selected)
                .Where(propValue => propValue?.Sender is not null && propValue.Value is true)
                .Subscribe(propValue => UpdateTeamSelection(propValue.Sender)));
        }

        private void SelectInitialTeam(Guid? displayTeamId)
        {
            var teamToSelect = displayTeamId is null ?
                Manager.TeamCards.First() :
                Manager.TeamCards.FirstOrDefault(team => team.TeamId == displayTeamId) ??
                Manager.TeamCards.First();

            teamToSelect.Select();
            teamToSelect.AssignedSections.CollectionChanged += SectionIndexChanging;

            SelectedTeam = teamToSelect;
        }

        private void AssignSection(SectionCardViewModel section)
        {
            SelectedTeam.AssignSection(section);
            section.AssignTeam(SelectedTeam);
        }

        private void UpdateTeamSelection(TeamCardViewModel selectedTeam)
        {
            Manager.TeamCards
                .Where(card => card.TeamId != selectedTeam.TeamId)
                .ForEach(card => card.Deselect());

            if (SelectedTeam is not null)
            {
                SelectedTeam.AssignedSections.CollectionChanged -= SectionIndexChanging;
            }

            SelectedTeam = selectedTeam;
            SelectedTeam.AssignedSections.CollectionChanged += SectionIndexChanging;
        }

        private void SectionIndexChanging(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is NotifyCollectionChangedAction.Remove)
            {
                _indexChangeBucket = new SectionIndexChangeBucket((SectionCardViewModel)e.OldItems[0], e.OldStartingIndex);
            }
            else if (e.Action is NotifyCollectionChangedAction.Add)
            {
                if (_indexChangeBucket is null)
                {
                    return;
                }

                _indexChangeBucket.NewIndex = e.NewStartingIndex;
            }
        }

        private void SectionIndexChanged()
        {
            if (_indexChangeBucket is null)
            {
                return;
            }

            var anchorSection = default(SectionCardViewModel);
            var newIndex = _indexChangeBucket.NewIndex;
            var assignedSectionsCount = SelectedTeam.AssignedSections.Count;
            var isToHigherPriority = _indexChangeBucket.NewIndex > _indexChangeBucket.OldIndex;

            if (newIndex is 0)
            {
                anchorSection = SelectedTeam.AssignedSections[1];
            }
            else if (newIndex == assignedSectionsCount - 1)
            {
                anchorSection = SelectedTeam.AssignedSections[assignedSectionsCount - 2];
            }
            else
            {
                anchorSection = isToHigherPriority ?
                    SelectedTeam.AssignedSections[newIndex - 1] :
                    SelectedTeam.AssignedSections[newIndex + 1];
            }

            Manager.MoveAssignedSection(_indexChangeBucket.Section, anchorSection);

            _indexChangeBucket = null;
        }

        public override void Dispose()
        {
            _indexChangeBucket = null;

            SelectedTeam = null;
            Manager = null;

            SectionIndexChangedCommand?.Dispose();
            SectionIndexChangedCommand = null;

            AssignSectionCommand?.Dispose();
            AssignSectionCommand = null;

            base.Dispose();
        }
    }
}