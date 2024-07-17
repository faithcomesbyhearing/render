using ReactiveUI;
using Render.Kernel.DragAndDrop;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public partial class TeamViewSectionCard
    {
        private const int MinimalAllowedWidth = 1500;
        private const int SectionTitleLabelDefaultWidth = 395;
        private const int SectionTitleLabelMinimalWidth = 340;
        public TeamViewSectionCard()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Section.Section.Title.Text,
                    v => v.SectionTitleLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Section.Section.Number,
                    v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Section.Section.ScriptureReference,
                    v => v.SectionReferenceLabel.Text));
                d(this.Bind(ViewModel, vm => vm.Section.Section.ScriptureReference,
                    v => v.SectionReferenceLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.IsSelected,
                    v => v.TeamViewSectionCardFrame.BorderColor, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.ShowCardOnModal,
                    v => v.TeamViewSectionCardComponent.IsVisible));
                TeamViewSectionCardFrame.SizeChanged += (sender, args) =>
                {
                    var isSmallScreen = Application.Current.MainPage.Width <= MinimalAllowedWidth;
                    SectionTitleLabel.SetValue(WidthRequestProperty,
                        isSmallScreen ? SectionTitleLabelMinimalWidth : SectionTitleLabelDefaultWidth);
                };
            });
        }

        private Color Selector(bool arg)
        {
            return arg ? ((ColorReference)ResourceExtensions.GetResourceValue("MainIconColor")).Color
                : ((ColorReference)ResourceExtensions.GetResourceValue("AlternateBackground")).Color;
        }

        private void DragGestureRecognizerEffect_OnDragStarting(object sender, DragAndDropEventArgs args)
        {
            args.Data = ViewModel?.Section;
        }

        private void TapGestureRecognizer_OnTapped(object sender, EventArgs e)
        {
            ViewModel?.ToggleSelectCommand.Execute().Subscribe();
        }
    }
}