using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Render.Components.TitleBar.MenuActions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Pages.Translator.DividePassagePage;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;


namespace Render.Components.TitleBar
{
    public class DividePassageActionViewModel : MenuActionViewModel
    {
        private Section Section { get; set; }
        private PassageNumber PassageNumber { get; }
        private Step Step { get; }

        public DividePassageActionViewModel(IViewModelContextProvider viewModelContextProvider, string pageName,
            Section section, PassageNumber passageNumber, Step step)
            : base("DividePassageAction", viewModelContextProvider, pageName)
        {
            Section = section;
            PassageNumber = passageNumber;
            Step = step;
            var color = ((ColorReference)ResourceExtensions.GetResourceValue("Option")) ?? new ColorReference();
            var imageSource = IconExtensions.BuildFontImageSource(Icon.DivisionOrCut, color.Color, 40);
            var title = AppResources.PassageDivide;
            var command = ReactiveCommand.CreateFromTask(NavigateToAsync);
            SetSources(imageSource, command, title);
        }

        private async Task<IRoutableViewModel> NavigateToAsync()
        {
            CloseMenu();

            var vm = await Task.Run(async () => await DividePassageDispatcher.GetDividePassagePageViewModelAsync(
                    Section, PassageNumber, ViewModelContextProvider, Step));
            return await NavigateTo(vm);
        }

        public override void Dispose()
        {
            Section = null;

            base.Dispose();
        }
    }
}
