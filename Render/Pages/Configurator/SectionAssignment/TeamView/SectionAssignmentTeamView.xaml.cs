using ReactiveUI;


namespace Render.Pages.Configurator.SectionAssignment.TeamView
{
    public partial class SectionAssignmentTeamView 
    {
        private const int MinimalAllowedWidth = 1500;
        private const int DefaultSpacingWidth = 80;
        private const int MinimalSpacingWidth = 5;
        private const int TeamsPanelMinimalWidth = 200;
        private const int TeamsPanelDefaultWidth = 250;
        private const int SectionPanelMinimalWidth = 440;
        private const int SectionPanelDefaultWidth = 520;
        public SectionAssignmentTeamView()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(x => x.ViewModel.Teams)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(TeamCollection);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(TeamCollection, x);
                        }
                    }));
                d(this.OneWayBind(ViewModel, vm => vm.SectionCards, 
                    v => v.SectionCollection.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.TeamAssignmentsViewModel, 
                    v => v.TeamAssignments.BindingContext));
                MainGrid.SizeChanged += OnSizeChanged;
            });
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            var isSmallScreen = Application.Current.MainPage.Width < MinimalAllowedWidth;
            LeftSpacing.SetValue(WidthRequestProperty, isSmallScreen ? MinimalSpacingWidth : DefaultSpacingWidth);
            RightSpacing.SetValue(WidthRequestProperty, isSmallScreen ? MinimalSpacingWidth : DefaultSpacingWidth);
            TeamsLayout.SetValue(WidthRequestProperty, isSmallScreen ? TeamsPanelMinimalWidth : TeamsPanelDefaultWidth);
            SectionPanel.SetValue(WidthRequestProperty,
                isSmallScreen ? SectionPanelMinimalWidth : SectionPanelDefaultWidth);
        }
    }
}