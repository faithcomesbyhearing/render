﻿using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Pages.AppStart.ProjectDownload
{
    public partial class ProjectDownload
    {
        public ProjectDownload()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                    v => v.TopLevelElement.FlowDirection));
                d(this.Bind(ViewModel, vm => vm.SearchString, v => v.SearchEntry.Text));

                d(this.WhenAnyValue(x => x.ViewModel.IsLoading)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(isLoading =>
                    {
                        if (isLoading)
                        {
                            SetNoProjectMessageVisible(false);
                            return;
                        }

                        var source = BindableLayout.GetItemsSource(ProjectsToDownloadCollection);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(ProjectsToDownloadCollection, ViewModel.ProjectCards);
                        }

                        SetProjectListVisible(ViewModel.HasProjectsToDownload);
                        SetNoProjectMessageVisible(ViewModel.HasProjectsToDownload is false);
                    }));
            });
        }

        private void SetProjectListVisible(bool visible)
        {
            SearchEntryBorder.SetValue(IsVisibleProperty, visible);
            ProjectsToDownloadScrollView.SetValue(IsVisibleProperty, visible);
        }

        private void SetNoProjectMessageVisible(bool visible)
        {
            NoProjectMessageContainer.SetValue(IsVisibleProperty, visible);
        }
    }
}