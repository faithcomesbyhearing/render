using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Pages.AppStart.Login
{
    public partial class AddProjectViaId
    {
        public AddProjectViaId()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));

                d(this.OneWayBind(ViewModel, vm => vm.ProjectIdValidationEntry, v => v.ProjectIdValidationEntry.BindingContext));
                d(this.BindCommandCustom(AddProjectButtonTap, v => v.ViewModel.DownloadProjectCommand));

                d(this.OneWayBind(ViewModel, vm => vm.AllowDownloadProjectCommand, v => v.AddProjectButton.Opacity, isEnabled => isEnabled ? 1 : 0.3));

                d(this.WhenAnyValue(x => x.ViewModel.AddProjectState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetPageState));
            });
        }

        private void SetPageState(AddProjectState addProjectState)
        {
            AddProjectWrapper.IsVisible = addProjectState == AddProjectState.None;
            AddViaFolderView.IsVisible = addProjectState != AddProjectState.None;
        }
    }
}