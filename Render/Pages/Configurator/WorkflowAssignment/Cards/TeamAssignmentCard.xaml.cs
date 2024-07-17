using System;
using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.DragAndDrop;
using Render.Pages.Configurator.WorkflowAssignment.Cards;

namespace Render.Pages.Configurator.WorkflowAssignment;

public partial class TeamAssignmentCard
{
    public TeamAssignmentCard()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.CardName.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.NameStack.IsVisible, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.ShowDeleteButton, v => v.RemoveTeamButton.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.UserCardViewModel, v => v.UserCard.BindingContext));
            d(this.OneWayBind(ViewModel, vm => vm.ShowUserCard, v => v.UserCardLayout.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.ShowUserCard, v => v.AddUserDropZone.IsVisible, Selector));
            d(this.OneWayBind(ViewModel, vm => vm.ShowUserCard, v => v.RemoveUserFromAssignmentButton.IsVisible));

            d(this.BindCommand(ViewModel, vm => vm.RemoveAssignmentCommand, v => v.RemoveUserFromAssignmentButtonGestureRecognizer));
            d(this.BindCommand(ViewModel, vm => vm.RemoveTeamCommand, v => v.RemoveTeamButtonGestureRecognizer));

            d(this.WhenAnyValue(v => v.ViewModel.Locked, v => v.ViewModel.ShowUserCard)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(((bool Locked, bool ShowUserCard) result) =>
                {
                    RemoveUserFromAssignmentButton.IsVisible = result.Locked is false && result.ShowUserCard;
                }));
        });
    }
    
    private bool Selector(string arg)
    {
        return !string.IsNullOrEmpty(arg);
    }

    private bool Selector(bool arg)
    {
        return !arg;
    }

    private void DropRecognizerEffect_Drop(object sender, DragAndDropEventArgs args)
    {
        if (!(args.Data is UserCardViewModel vm)) return;
        ViewModel?.OnDrop(vm.User);
    }
}