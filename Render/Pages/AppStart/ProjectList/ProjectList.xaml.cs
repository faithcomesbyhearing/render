using System.Reactive.Linq;
using ReactiveUI;
using Render.Resources;
using Render.Resources.Localization;

namespace Render.Pages.AppStart.ProjectList
{
    public partial class ProjectList
    {
        public ProjectList()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                    v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.ProjectList, v => v.ProjectsCollection.ItemsSource));
                d(this.WhenAnyValue(x => x.ViewModel.HasProjects)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetStatusForEmptyCollection));
            });
        }

        private void SetStatusForEmptyCollection(bool? hasProjects)
        {
            ProjectsCollection.SetValue(IsVisibleProperty, false);
            NoProjectMessageContainer.SetValue(IsVisibleProperty, true);
            switch (hasProjects)
            {
                case true:
                    ProjectsCollection.SetValue(IsVisibleProperty, true);
                    NoProjectMessageContainer.SetValue(IsVisibleProperty, false);
                    break;
                case false:
                    Icon.Text = IconExtensions.BuildFontImageSource(Render.Resources.Icon.EmptyProject).Glyph;
                    MessageTitle.Text = AppResources.EmptyCollectionMessageTitle.Replace(".", string.Empty);
                    MessageText.Text = AppResources.EmptyCollectionMessage;
                    break;
                case null:
                    Icon.Text = IconExtensions.BuildFontImageSource(Render.Resources.Icon.NoOffloadProject).Glyph;
                    MessageTitle.Text = AppResources.EmptyOffloadCollectionMessage;
                    MessageText.Text = AppResources.EmptyCollectionMessageTitle;
                    break;
            }
        }
    }
}