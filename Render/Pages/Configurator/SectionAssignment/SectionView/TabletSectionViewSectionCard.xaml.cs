using ReactiveUI;
using Render.Kernel;
using Render.Kernel.DragAndDrop;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public partial class TabletSectionViewSectionCard
    {
        public TabletSectionViewSectionCard()
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
                d(this.OneWayBind(ViewModel, vm => vm.ShowTeam,
                    v => v.UserStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ShowTeam,
                    v => v.DropRectangle.IsVisible, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.SectionViewTeamCardViewModel.Name,
                    v => v.UserNameLabel.Text));
                d(this.BindCommand(ViewModel, vm => vm.RemoveTeamCommand,
                    v => v.RemoveButtonGestureRecognizer));
                d(this.BindCommand(ViewModel, vm => vm.OnDragLeaveCommand, 
                    v => v.SectionDragOverRecognizer, nameof(DropGestureRecognizer.DragLeave)));
                d(this.OneWayBind(ViewModel, vm => vm.ShowDragOverSection,
                    v => v.AddSectionAfter.IsVisible));
            });
        }
        private bool Selector(bool arg)
        {
            return !arg;
        }

        private void TeamDropRecognizerEffect_OnDrop(object sender, DragAndDropEventArgs args)
        {
            if(!(args.Data is NewSectionViewTeamCardViewModel team)) return;
            ViewModel?.AssignTeamToSectionCommand.Execute(team).Subscribe(Stubs.ActionNop, Stubs.ExceptionNop);
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
            if(!(args.Data is TeamSectionAssignment)) return;
            ViewModel?.OnDragOver();
        }
    }
}