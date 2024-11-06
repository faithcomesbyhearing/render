using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.SectionAssignment.Cards.Team
{
    public partial class SelectableTeamCard
    {
        public SelectableTeamCard()
        {
            InitializeComponent();
            
            var selectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
            var nonSelectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("Transparent")).Color;
            var selectedTextColor = ((ColorReference)ResourceExtensions.GetResourceValue("SecondaryText")).Color;
            var nonTextSelectedColor = ((ColorReference)ResourceExtensions.GetResourceValue("MainText")).Color;
            var selectedBorderColor = ((ColorReference)ResourceExtensions.GetResourceValue("LightBackgroundText")).Color;
            var selecteedBrush = new SolidColorBrush(selectedBorderColor);
            var nonSelectedBrush = new SolidColorBrush(nonSelectedColor);

            this.WhenActivated(d =>
            {
               d(this.OneWayBind(ViewModel, vm => vm.Name, v => v.UserNameLabel.Text));
               d(this.OneWayBind(ViewModel, vm => vm.AssignedSections.Count, v => v.CountLabel.Text));
               d(this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.UserTap));
               d(this
                   .WhenAnyValue(x => x.ViewModel.Selected)
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(isSelected =>
                   {
                       TeamUserFrame.BackgroundColor = isSelected ? selectedColor : nonSelectedColor;
                       UserNameLabel.TextColor = isSelected ? selectedTextColor : nonTextSelectedColor;
                       TeamUserFrame.Stroke = isSelected ? selecteedBrush : nonSelectedBrush;
                       TeamCountFrame.HorizontalOptions = isSelected ? LayoutOptions.End : LayoutOptions.Center;
                       TeamSeparatorLine.IsVisible = ViewModel.TeamNumber != 1;
                   }));
            });
        }
    }
}