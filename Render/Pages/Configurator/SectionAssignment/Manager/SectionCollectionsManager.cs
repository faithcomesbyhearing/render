using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using DynamicData;
using DynamicData.Binding;
using Render.Pages.Configurator.SectionAssignment.Utils;
using Render.Pages.Configurator.SectionAssignment.Cards.Section;
using Render.Pages.Configurator.SectionAssignment.Cards.Team;
using Render.Pages.Configurator.SectionAssignment.Factory;
using Render.Extensions;

namespace Render.Pages.Configurator.SectionAssignment.Manager;

public class SectionCollectionsManager : ReactiveObject, IDisposable
{
    private List<IDisposable> _disposables = new();
    private SectionIndexChangeBucket _priorityChangeBucket;

    [Reactive]
    public ObservableCollection<SectionCardViewModel> AllSectionCards { get; private set; }

    [Reactive]
    public ObservableCollection<SectionCardViewModel> UnassignedSectionCards { get; private set; }

    [Reactive]
    public ObservableCollection<TeamCardViewModel> TeamCards { get; private set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> SectionPriorityChangedCommand { get; private set; }

    public SectionCollectionsManager(SectionAssignmentCards cards)
    {
        TeamCards = new(cards.TeamCards.OrderBy(team => team.Name));

        AllSectionCards = new(cards.SectionCards
            .OrderBy(card => card.Priority)
            .ThenBy(card => card.Number));

        UnassignedSectionCards = new(AllSectionCards
            .Where(card => card.AssignedTeamViewModel is null));

        SectionPriorityChangedCommand = ReactiveCommand.Create(SectionPriorityChanged);

        UpdateSectionPriorities();
        AddSubscriptions();
    }

    public void MoveAssignedSection(SectionCardViewModel sectionToMove, SectionCardViewModel anchorSection)
    {
        if (this.CanMoveSection(sectionToMove, anchorSection) is false)
        {
            return;
        }

        var moveIndex = AllSectionCards.IndexOf(anchorSection);
        var currentIndex = AllSectionCards.IndexOf(sectionToMove);

        AllSectionCards.Move(currentIndex, moveIndex);
        UpdateSectionPriorities();
    }

    public void UpdateUnassignedSections(SectionCardViewModel section)
    {
        if (section.AssignedTeamViewModel is not null)
        {
            UnassignedSectionCards.Remove(section);
        }
        else
        {
            UnassignedSectionCards.Insert(this.GetInsertIndexByNumber(section), section);
        }
    }
    
    private void AddSubscriptions()
    {
        AllSectionCards.CollectionChanged += SectionPriorityChanging;

        _disposables.Add(AllSectionCards
            .ToObservableChangeSet()
            .WhenPropertyChanged(card => card.AssignedTeamViewModel)
            .Skip(AllSectionCards.Count)
            .Subscribe(propValue => UpdateUnassignedSections(propValue.Sender)));
    }

    private void SectionPriorityChanging(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Remove)
        {
            _priorityChangeBucket = new SectionIndexChangeBucket((SectionCardViewModel)e.OldItems[0], e.OldStartingIndex);
        }
        else if (e.Action is NotifyCollectionChangedAction.Add)
        {
            _priorityChangeBucket.NewIndex = e.NewStartingIndex;
        }
    }

    private void SectionPriorityChanged()
    {
        if (_priorityChangeBucket is null)
        {
            return;
        }

        UpdateSectionPriorities();

        _priorityChangeBucket.Section.AssignedTeamViewModel?.MoveSection(
            sectionToMove: _priorityChangeBucket.Section, 
            isToHigerPriority: _priorityChangeBucket.OldIndex < _priorityChangeBucket.NewIndex);

        _priorityChangeBucket = null;
    }

    private void UpdateSectionPriorities()
    {
        for (int i = 0; i < AllSectionCards.Count; i++)
        {
            AllSectionCards[i].Priority = i;
        }
    }

    public void Dispose()
    {
        SectionPriorityChangedCommand?.Dispose();
        SectionPriorityChangedCommand = null;

        AllSectionCards.CollectionChanged -= SectionPriorityChanging;
        AllSectionCards.ForEach(card => card.Dispose());
        AllSectionCards.Clear();
        AllSectionCards = null;

        UnassignedSectionCards.Clear();
        UnassignedSectionCards = null;

        TeamCards.ForEach(card => card.Dispose());
        TeamCards.Clear();
        TeamCards = null;

        _disposables.ForEach(disposable => disposable.Dispose());
        _disposables.Clear();
        _disposables = null;
    }
}