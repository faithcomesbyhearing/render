using ReactiveUI;

namespace Render.Pages.Configurator.SectionAssignment.SectionView
{
    public partial class SectionAssignmentSectionView
    {
        private const int MinimalAllowedWidth = 1500;
        private const int DefaultSpacing = 20;
        private const int LeftSpaceLayoutWidth = 330;
        private const int RightSpaceLayoutWidth = 60;
        public SectionAssignmentSectionView()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.SectionCards, 
                   v => v.SectionCollection.ItemsSource));
               d(this.OneWayBind(ViewModel, vm => vm.TeamCards.Items, 
                   v => v.TeamCollection.ItemsSource));
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
    }
}