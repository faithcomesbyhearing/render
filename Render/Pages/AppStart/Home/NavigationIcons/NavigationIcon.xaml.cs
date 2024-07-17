using ReactiveUI;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;
using Render.Resources.Styles;

namespace Render.Pages.AppStart.Home.NavigationIcons;

public partial class NavigationIcon
    {
        public NavigationIcon()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.StageNumber, 
                   v => v.StageNumberFrame.IsVisible, Selector));
                d(this.OneWayBind(ViewModel, vm => vm.StageNumber, 
                   v => v.Number.Text));
                d(this.BindCommandCustom(Tap, v => v.ViewModel.NavigateToPageCommand));
                d(this.OneWayBind(ViewModel, vm => vm.IconImageGlyph,
                   v => v.IconSource.Text));
               d(this.OneWayBind(ViewModel, vm => vm.IconName,
                   v => v.Title.AutomationId, SetAutomationIdForALabel));
               d(this.OneWayBind(ViewModel, vm => vm.IconName,
                   v => v.IconSource.AutomationId, SetAutomationIdForAnIcon));
               d(this.OneWayBind(ViewModel, vm => vm.ActionState, 
                   v => v.IconSource.TextColor, Selector));
               d(this.OneWayBind(ViewModel, vm => vm.Title, 
                   v => v.Title.Text));
               d(this.OneWayBind(ViewModel, vm => vm.ActionState,
                   v => v.IconSource.Opacity,  x => x == ActionState.Inactive ? 0.3 : 1));
            });
        }

        private string SetAutomationIdForALabel(string arg)
        {
            return $"{arg}NavigationLabel";
        }

        private string SetAutomationIdForAnIcon(string arg)
        {
            return $"{arg}NavigationImage";
        }
        
        private Color Selector(ActionState arg)
        {
            switch (arg)
            {
                case ActionState.Inactive:
                case ActionState.Optional:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("Option")).Color;
                case ActionState.Required:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("Required")).Color;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private bool Selector(int arg)
        {
            return arg > 0;
        }
    }