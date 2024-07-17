using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public partial class TeamViewTeamCard
    {
        public TeamViewTeamCard()
        {
            InitializeComponent();
            
            var selectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
            var nonSelectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("Transparent")).Color;
            var selectedTextColor = ((ColorReference)ResourceExtensions.GetResourceValue("SecondaryText")).Color;
            var nonTextSelectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("MainText")).Color;
            var selectedBorderColor = ((ColorReference)ResourceExtensions.GetResourceValue("LightBackgroundText")).Color;

            this.WhenActivated(d =>
            {
               d(this.OneWayBind(ViewModel, vm => vm.TeamName,
                   v => v.UserNameLabel.Text));
               d(this.OneWayBind(ViewModel, vm => vm.Count,
                   v => v.CountLabel.Text));
               d(this.OneWayBind(ViewModel, vm => vm.CountString,
                   v => v.CountLabel.Text));
               d(this.BindCommand(ViewModel, vm => vm.SelectCommand, 
                   v => v.UserTap));
               d(this.WhenAnyValue(x => x.ViewModel.Selected)
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(x =>
                   {
                       TeamUserFrame.BackgroundColor = x ? selectedColor : nonSelectedColor;
                       UserNameLabel.TextColor = x ? selectedTextColor : nonTextSelectedColor;
                       TeamUserFrame.BorderColor = x ? selectedBorderColor : nonSelectedColor;
                       TeamCountFrame.HorizontalOptions = x ? LayoutOptions.End : LayoutOptions.Center;
                       TeamSeparatorLine.IsVisible = ViewModel.Team.Team.TeamNumber != 1;
                   }));
            });
        }
    }
}