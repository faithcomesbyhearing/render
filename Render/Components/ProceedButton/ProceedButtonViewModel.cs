using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Resources;

namespace Render.Components.ProceedButton
{
    public class ProceedButtonViewModel : ViewModelBase
    {
        [Reactive] public bool ProceedActive { get; set; }

        [Reactive] public bool IsCheckMarkIcon { get; set; }

        [Reactive] public FontImageSource Icon { get;  set; }

        [Reactive] public Color BackgroundColor { get; set; }

        [Reactive] public Color BorderColor { get;  set; }
        
        //Please use the SetCommand() method.
        //You can read about this change here: 
        //https://dev.azure.com/FCBH/Software%20Development/_wiki/wikis/Vessel.wiki/774/Migration-from-Xamarin?anchor=proceed-button-command
        [Reactive] public ReactiveCommand<Unit, IRoutableViewModel> NavigateToPageCommand { get; private set; } 

        public ProceedButtonViewModel(IViewModelContextProvider viewModelContextProvider,
            bool isCheckMarkIcon = false) :
            base("ProceedButton", viewModelContextProvider)
        {
            //Set ProceedActive to true, because at this point there are no actions in the list
            ProceedActive = true;
            IsCheckMarkIcon = isCheckMarkIcon;
            SetIcon(ProceedActive);
            Disposables.Add(this.WhenAnyValue(x => x.ProceedActive)
                .Subscribe(SetIcon));
            Disposables.Add(this.WhenAnyValue(x => x.IsCheckMarkIcon)
                .Subscribe(SetIcon));
        }

        public void SetCommand(Func<Task<IRoutableViewModel>> navigateToPageAsync)
        {
            NavigateToPageCommand = ReactiveCommand.CreateFromTask(navigateToPageAsync, canExecute: this.WhenAnyValue(x => x.ProceedActive));
        }

        private void SetIcon(bool active)
        {
            if (IsCheckMarkIcon)
            {
                BackgroundColor = ResourceExtensions.GetColor(ProceedActive ? "Required" : "ApproveProceedButtonInactiveBackground");
                BorderColor = BackgroundColor;
                Icon = IconExtensions.BuildFontImageSource(
                    Resources.Icon.NavBarCheckmark,
                    ResourceExtensions.GetColor(ProceedActive ? "Option" : "SecondaryText"));
                return;
            }

            BackgroundColor = ResourceExtensions.GetColor("Option");
            BorderColor = BackgroundColor;
            var chevron = FlowDirection == FlowDirection.RightToLeft
                ? Resources.Icon.ChevronLeft : Resources.Icon.ChevronRight;
            Icon = IconExtensions.BuildFontImageSource(chevron, ResourceExtensions.GetColor("SecondaryText"));
        }

        public override void Dispose()
        {
            NavigateToPageCommand?.Dispose();
            NavigateToPageCommand = null;

            Icon = null;
            base.Dispose();
        }
    }
}