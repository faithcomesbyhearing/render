using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Render.Kernel;
using Render.Pages.Settings.AudioExport;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;


namespace Render.Components.TitleBar.MenuActions
{
    public class AudioExportActionViewModel : MenuActionViewModel
    {
        private readonly Guid _projectId;

        public AudioExportActionViewModel(IViewModelContextProvider viewModelContextProvider, Guid projectId, string pageName)
            : base("AudioExportAction", viewModelContextProvider, pageName)
        {
            _projectId = projectId;
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")) ?? new ColorReference();
            var imageSource = IconExtensions.BuildFontImageSource(Icon.ExportAudio, color.Color, 40);
            var title = AppResources.ExportAudio;
            var command = ReactiveCommand.CreateFromTask(NavigateToAsync);
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
			CloseMenu();

			var vm = await Task.Run(async () => await AudioExportPageViewModel.CreateAsync(ViewModelContextProvider, _projectId));
            return await NavigateTo(vm);
        }
    }
}
