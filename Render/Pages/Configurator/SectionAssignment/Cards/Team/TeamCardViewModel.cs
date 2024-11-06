using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Team
{
    public class TeamCardViewModel : ViewModelBase
    {
        public required Guid TeamId { get; init; }

        public int TeamNumber { get; init; }

        [Reactive]
        public string Name { get; init; }

        [Reactive]
        public bool Selected { get; private set; }

        [Reactive]
        public ObservableCollection<SectionCardViewModel> AssignedSections { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectCommand { get; private set; }

        public TeamCardViewModel(IViewModelContextProvider viewModelContextProvider)
            : base(nameof(TeamCardViewModel), viewModelContextProvider)
        {
            AssignedSections = new ObservableCollection<SectionCardViewModel>();
            SelectCommand = ReactiveCommand.Create(Select);
        }

        public void AssignSection(SectionCardViewModel section)
        {
            if (AssignedSections.Contains(section) is true)
            {
                return;
            }

            AssignedSections.Insert(this.GetInsertIndexByPriority(section), section);
        }

        public void RemoveSectionAssignement(SectionCardViewModel sectionViewModel)
        {
            AssignedSections.Remove(sectionViewModel);
        }

        public void MoveSection(SectionCardViewModel sectionToMove, bool isToHigerPriority)
        {
            var currentIndex = AssignedSections.IndexOf(sectionToMove);
            if (this.CanMoveSection(sectionToMove, currentIndex) is false)
            {
                return;
            }

            var targetIndex = this.GetMoveIndexByPriority(sectionToMove, isToHigerPriority);
            if (currentIndex != targetIndex)
            {
                AssignedSections.Move(currentIndex, targetIndex);
            }
        }

        public void Select()
        {
            Selected = true;
        }

        public void Deselect()
        {
            Selected = false;
        }

        public override void Dispose()
        {
            SelectCommand?.Dispose();
            SelectCommand = null;

            AssignedSections.Clear();
            AssignedSections = null;

            base.Dispose();
        }
    }
}