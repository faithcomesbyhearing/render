using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Splat;

namespace Render.Components.TitleBar.MenuActions
{
    public abstract class MenuActionViewModel : ViewModelBase, IMenuActionViewModel
    {
        public string Glyph { get; private set; }

        public ReactiveCommand<Unit, IRoutableViewModel> Command { get; private set; }

        [Reactive] public bool IsActionExecuting { get; set; }

        [Reactive] public bool CanActionExecute { get; set; } = true;

        private IViewModelContextProvider _viewModelContextProvider;

        public string Title { get; private set; }
        private string PageName;

        protected MenuActionViewModel(string urlPath, IViewModelContextProvider viewModelContextProvider, string pageName) : base
        (urlPath,
            viewModelContextProvider)
        {
            PageName = pageName;
            _viewModelContextProvider = viewModelContextProvider;
        }

        public MenuActionViewModel(FontImageSource imageSource, 
            ReactiveCommand<Unit, IRoutableViewModel> command, 
            string title,
            string urlPath,
            IViewModelContextProvider viewModelContextProvider) : base(urlPath, viewModelContextProvider)
        {
            SetSources(imageSource, command, title);
        }
        
        public void SetCommandCondition(Func<Task<bool>> condition)
        {
            Command = Command.AddCondition(condition);
        }
        
        //This setter is for the unit test because this view model is now an abstract class that is tested
        // in the child action view models.
        public void SetCommand(ReactiveCommand<Unit, IRoutableViewModel> command)
        {
            Command = command;
        }

        protected void SetSources(FontImageSource imageSource, ReactiveCommand<Unit, IRoutableViewModel> command, 
            string title)
        {
            Glyph = imageSource?.Glyph;
            Title = title;
            Command = command;
        }

        protected void CloseMenu() => _viewModelContextProvider.GetMenuPopupService().Close();
        
        public override void Dispose()
        {
            Command?.Dispose();
            base.Dispose();
        }
    }
}