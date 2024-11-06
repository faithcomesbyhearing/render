using ReactiveUI;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public partial class ProcessesView
    {
        private const int infoCardWidth = 420;
        private const int sectionCollectionWidth = 960;

        public ProcessesView()
        {
            InitializeComponent();
            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.UnassignedSectionCollectionAsStageCardViewModel,
                    v => v.UnassignedSectionCollectionAsStageCardView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.UnassignedSectionCollectionAsStageCardViewModel.HasSections,
                    v => v.UnassignedSectionCollectionAsStageCardView.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.ApprovedSectionCollectionAsStageCardViewModel,
                    v => v.ApprovedSectionCollectionAsStageCardView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.Section.Number,
                    v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Section.Title.Text,
                    v => v.SectionTitle.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.TotalPassages,
                    v => v.TotalPassages.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.Reference,
                    v => v.Reference.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.TotalVerses,
                    v => v.TotalVerses.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.CurrentStep,
                    v => v.Process.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.CurrentUsername,
                    v => v.User.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.DraftedBy,
                    v => v.DraftedBy.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.ApprovedBy,
                    v => v.ApprovedBy.Text, EmptyStringConverter));
                d(this.OneWayBind(ViewModel, vm => vm.ShowSectionInformation,
                    v => v.InfoModal.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.IsNotAssignedAnything, v => v.NoSectionAssigned.IsVisible));
                d(this.WhenAnyValue(v => v.ViewModel.SectionInfoPlayers)
                    .Subscribe(observableCollection =>
                    {
                        BindableLayout.SetItemsSource(BarPlayerCollection, observableCollection);
                    }));
                d(this.BindCommand(ViewModel, vm => vm.CloseInfoCommand,
                    v => v.CloseGestureRecognizer));
                d(this.WhenAnyValue(x => x.ViewModel.StageCards)
                    .Subscribe(x => { BindableLayout.SetItemsSource(StageCollection, x); }));
                d(this.WhenAnyValue(x => x.ViewModel.SectionSelected)
                    .Subscribe(source => { InfoModalScrollView.ScrollToAsync(0, 0, false); }));
                d(this.WhenAnyValue(x => x.ViewModel.IsNotAssignedAnything)
                    .Subscribe(empty =>
                    {
                        NoSectionAssigned.IsVisible = empty;
                        OuterScrollView.IsVisible = !NoSectionAssigned.IsVisible;
                    }));
                d(this.WhenAnyValue(x => x.ViewModel.HasConflict).Subscribe(hasConflict =>
                   {
                       ConflictIcon.IsVisible = hasConflict;
                   }));
            });

            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            // smooth transition from RightToLLeft and vice versa
            var width = 2 * infoCardWidth + sectionCollectionWidth;

            if (Window.Width < width)
            {
                var windowWidth = Window.Width - infoCardWidth;
                int cardWidth = windowWidth > 0 ? (int)windowWidth : 0;
                var currentWidth = cardWidth - sectionCollectionWidth;

                var thickness = new Thickness(currentWidth > 0 ? currentWidth : 0, 0, infoCardWidth, 0);

                OuterScrollView.BatchBegin();
                OuterScrollView.WidthRequest = cardWidth;
                OuterScrollView.Margin = thickness;
                OuterScrollView.BatchCommit();

                NoSectionAssigned.BatchBegin();
                NoSectionAssigned.WidthRequest = cardWidth;
                NoSectionAssigned.Margin = thickness;
                NoSectionAssigned.BatchCommit();
            }
            else
            {
                var thickness = new Thickness(infoCardWidth, 0);

                OuterScrollView.BatchBegin();
                OuterScrollView.WidthRequest = sectionCollectionWidth;
                OuterScrollView.Margin = thickness;
                OuterScrollView.BatchCommit();

                NoSectionAssigned.BatchBegin();
                NoSectionAssigned.WidthRequest = sectionCollectionWidth;
                NoSectionAssigned.Margin = thickness;
                NoSectionAssigned.BatchCommit();
            }
        }

        private object EmptyStringConverter(object value)
        {
            if (string.IsNullOrEmpty((string)value))
            {
                return "-";
            }

            return value;
        }
    }
}