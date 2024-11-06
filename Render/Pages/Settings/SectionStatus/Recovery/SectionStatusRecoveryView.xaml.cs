using ReactiveUI;
using Render.Resources.Localization;

namespace Render.Pages.Settings.SectionStatus.Recovery;

 public partial class SectionStatusRecoveryView
    {
        public SectionStatusRecoveryView()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.SearchString, v => v.SearchEntry.Text));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedCard.Section.ScriptureReference, 
                    v => v.SectionScriptureReference.Text));
                d(this.OneWayBind(ViewModel, vm => vm.SectionTitleLabel, 
                    v => v.SectionLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedCard.Section.Title.Text, 
                    v => v.SectionTitle.Text));          
                d(this.OneWayBind(ViewModel, vm => vm.SelectedCard.Section.Number, 
                    v => v.SectionNumber.Text));
                d(this.BindCommand(ViewModel, vm => vm.SelectSnapshotCommand,
                    v => v.SelectSnapshotGestureRecognizer));
                d(this.BindCommand(ViewModel, vm => vm.DeleteSnapshotsCommand,
                    v => v.ClearSnapshotGestureRecognizer));
                d(this.OneWayBind(ViewModel, vm => vm.ShowBarPlayer, 
                    v => v.SectionPlayerStack.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.SectionPlayer, 
                    v => v.SectionPlayer.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ResetCard, 
                    v => v.ResetCard.BindingContext));
                d(this.WhenAnyValue(x => x.ViewModel.StageName)
                    .Subscribe(stageName => ConflictedStageName.Text = string.Format(AppResources.SnapshotConflict, stageName)));
                
                d(this.OneWayBind(ViewModel, vm => vm.SectionApproved,
                    v => v.ApprovedLabel.IsVisible));

                d(this.WhenAnyValue(x => x.ViewModel.SectionCardViewModels)
                    .Subscribe(v =>
                    {
                        var source = BindableLayout.GetItemsSource(Sections);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(Sections, v);
                        }
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.SnapshotCardViewModels)
                    .Subscribe(v =>
                    {
                        var source = BindableLayout.GetItemsSource(Snapshots);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(Snapshots, v);
                        }
                    }));

                d(this.WhenAnyValue(x => x.ViewModel.ConflictMode, x => x.ViewModel.ApprovalPresent)
                    .Subscribe(tuple =>
                    {
                        if (tuple.Item1)
                        {
                            SelectionView.IsVisible = false;
                            ConflictView.IsVisible = true;
                            ConflictIcon.IsVisible = true;
                            ApprovedIcon.IsVisible = false;
                            SectionPlayerStack.IsVisible = false;
                        }
                        else
                        {
                            SelectionView.IsVisible = true;
                            ConflictView.IsVisible = false;
                            ConflictIcon.IsVisible = false;
                            ApprovedIcon.IsVisible = tuple.Item2;
                            SectionPlayerStack.IsVisible = ViewModel.ShowBarPlayer;
                        }
                    }));
            });

            SearchEntry.TextChanged += OnEntryTextChanged;
        }
        
        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(e.NewTextValue)) 
            { 
                bool isValid = e.NewTextValue.ToCharArray().All(char.IsDigit); //Make sure all characters are numbers

                ((Entry)sender).Text = isValid ? e.NewTextValue : e.NewTextValue.Remove(e.NewTextValue.Length - 1);
            }
        }
    }