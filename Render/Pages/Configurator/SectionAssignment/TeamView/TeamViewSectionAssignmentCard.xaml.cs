using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Kernel.DragAndDrop;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public partial class TeamViewSectionAssignmentCard
    {
        private const double SectionCardHeight = 105d;
        private const double SectionDropZoneHeight = 75d;
        public TeamViewSectionAssignmentCard()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Assignment.Section.Number, 
                    v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Assignment.Section.Title.Text, 
                    v => v.SectionTitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Assignment.Section.ScriptureReference,
                    v => v.SectionReferenceLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSection,
                    v => v.SectionStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSection,
                    v => v.DropRectangle.IsVisible, Selector));
                d(this.BindCommand(ViewModel, vm => vm.RemoveAssignmentCommand,
                    v => v.RemoveButtonGestureRecognizer));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSectionDropZone)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(UpdateDropZoneLayout));
                d(this.BindCommand(ViewModel, vm => vm.OnDragLeaveCommand, 
                    v => v.SectionDragOverRecognizer, nameof(DropGestureRecognizer.DragLeave)));
                d(this.WhenAnyValue(x => x.ViewModel.ShowSection)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        if (!x)
                        {
                            Thread.Sleep(100);
                        }
                    }));
            });
        }

        private bool Selector(bool arg)
        {
            return !arg;
        }
        private void TeamDropRecognizerEffect_OnDrop(object sender, DragAndDropEventArgs args)
        {
            if(!(args.Data is TeamSectionAssignment team)) return;
            ViewModel?.AssignSectionCommand.Execute(team)
                .Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);
        }
        
        private void SectionDragRecognizerEffect_OnDragStarting(object sender, DragAndDropEventArgs args)
        {
            args.Data = ViewModel?.Assignment;
        }

        private void SectionDropRecognizerEffect_OnDrop(object sender, DragAndDropEventArgs args)
        {
            if(!(args.Data is TeamSectionAssignment team)) return;
            ViewModel?.OnSectionDropCommand.Execute(team).Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);
        }

        private void DropGestureRecognizerEffect_OnDragOver(object sender, DragAndDropEventArgs args)
        {
            if(!(args.Data is TeamSectionAssignment team)) return;
            if (team.Team == null && team.Section != null) return; // don't show plus icon if we are dragging sections over to assign
            ViewModel?.OnDragOver();
        }

        private void UpdateDropZoneLayout(bool showSectionDropZone)
        {
            AddSectionLabel.IsVisible = showSectionDropZone;
            AddSectionLine.IsVisible = showSectionDropZone;
            TopElement.HeightRequest = SectionCardHeight + (ViewModel.ShowSectionDropZone ? SectionDropZoneHeight : 0);
        }
    }
}