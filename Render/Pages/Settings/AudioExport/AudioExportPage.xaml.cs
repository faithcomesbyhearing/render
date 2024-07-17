using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources.Localization;

namespace Render.Pages.Settings.AudioExport
{
    public partial class AudioExportPage
    {
        public AudioExportPage()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.StageView, v => v.StageView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedSortOption, v => v.StageView.IsVisible, ShowStageViewSelector));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedSortOption, v => v.AllSectionsView.IsVisible, ShowAllSectionsSelector));
                d(this.BindCommandCustom(ExportAudioGuGestureRecognizer, v => v.ViewModel.ExportCommand));
                d(this.WhenAnyValue(x => x.ViewModel.Status)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetProgressBarStatus));
                d(this.OneWayBind(ViewModel, vm => vm.SortOptions, v => v.SortPicker.ItemsSource));
                d(this.Bind(ViewModel, vm => vm.SelectedSortOption, v => v.SortPicker.SelectedItem));
                d(this.WhenAnyValue(x => x.ViewModel.SelectedSortOption)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(sortOption =>
                    {
                        if (ShowStageViewSelector(sortOption))
                        {
                            foreach (var stage in ViewModel.StageView.Stages.Items)
                            {
                                var sections = stage.SectionCards;
                                for (var index = 0; index < sections.Count; index++)
                                {
                                    sections[index].ShowSeparator = index != sections.Count - 1;
                                }
                            }
                        }
                        else
                        {
                            var sections = ViewModel.AllSectionsView.Sections;
                            for (var index = 0; index < sections.Count; index++)
                            {
                                sections[index].ShowSeparator = index != sections.Count - 1;
                            }
                        }
                    }));
                d(this.OneWayBind(ViewModel, vm => vm.EnableExportButton, v => v.ExportButton.IsEnabled));
                d(this.OneWayBind(ViewModel, vm => vm.EnableExportButton, v => v.ExportButton.Opacity, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.AllSectionsView, v => v.AllSectionsView.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.ExportPercent, v => v.ProgressBar.ProgressPercent));
                d(this.OneWayBind(ViewModel, vm => vm.ExportPercent, v => v.ProgressLabel.Text, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.ExportedString, v => v.ExportingLabel.Text));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.LoadingView.IsVisible));
            });
        }

        private string Selector(double arg)
        {
            return $"{arg:P0}";
        }

        private double Selector(bool arg)
        {
            return arg ? 1.0 : 0.3;
        }

        private bool ShowStageViewSelector(string arg)
        {
            return string.Equals(arg, AppResources.LastCompletedStage);
        }

        private bool ShowAllSectionsSelector(string arg)
        {
            return string.Equals(arg, AppResources.AllSections);
        }

        private void SetProgressBarStatus(ExportingStatus status)
        {
            SuccessIcon.IsVisible = false;
            switch (status)
            {
                case ExportingStatus.None:
                    ProgressStack.IsVisible = false;
                    break;
                case ExportingStatus.Exporting:
                    ProgressStack.IsVisible = true;
                    break;
                case ExportingStatus.Completed:
                    SuccessIcon.IsVisible = true;
                    ExportingLabel.Text = AppResources.ExportComplete;
                    break;
            }
        }
    }
}