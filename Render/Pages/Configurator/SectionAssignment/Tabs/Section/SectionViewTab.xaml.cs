using ReactiveUI;

namespace Render.Pages.Configurator.SectionAssignment.Tabs.Section
{
    public partial class SectionViewTab
    {
        private const int MinimalAllowedWidth = 1500;
        private const int DefaultSpacing = 20;
        private const int LeftSpaceLayoutWidth = 330;
        private const int RightSpaceLayoutWidth = 60;
        
        public SectionViewTab()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Manager.AllSectionCards, v => v.SectionCollection.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.Manager.TeamCards, v => v.TeamCollection.ItemsSource));
                d(this.BindCommand(ViewModel, vm => vm.Manager.SectionPriorityChangedCommand, v => v.SectionCollection, nameof(SectionCollection.ReorderCompleted)));
            });

            MainGrid.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            var isSmallScreen = Application.Current.MainPage.Window.Width <= MinimalAllowedWidth;
            if (isSmallScreen)
            {
                LeftSpaceLayout.WidthRequest = DefaultSpacing;
                RightSpaceLayout.WidthRequest = DefaultSpacing;
                TeamsCollectionLayout.Margin = new Thickness(15, 0, 10, 0);
                TeamCollection.Margin = new Thickness(0, 15);
            }
            else
            {
                LeftSpaceLayout.WidthRequest = LeftSpaceLayoutWidth;
                RightSpaceLayout.WidthRequest = RightSpaceLayoutWidth;
                TeamsCollectionLayout.Margin = new Thickness(30, 0);
                TeamCollection.Margin = new Thickness(0, 30);
            }
        }

        protected override void Dispose(bool disposing)
        {
            MainGrid.SizeChanged -= OnSizeChanged;
            
            TeamCollection.ItemsSource = null;
            TeamCollection.ClearLogicalChildren();

            SectionCollection.ItemsSource = null;
            SectionCollection.ClearLogicalChildren();

            base.Dispose(disposing);
        }
    }
}