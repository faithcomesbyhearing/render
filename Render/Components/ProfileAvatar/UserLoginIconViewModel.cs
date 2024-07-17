using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Users;
using System.Reactive;

namespace Render.Components.ProfileAvatar
{
    public class UserLoginIconViewModel : ViewModelBase, IUserLoginIconViewModel
    {
        [Reactive]
        public IUser User { get; private set; }

        [Reactive]
        public bool Selected { get; set; }

        [Reactive]
        public bool IsRenderUser { get; private set; }

        public ReactiveCommand<Unit, Unit> OnSelectUser { get; }

        public UserLoginIconViewModel(IUser user, IViewModelContextProvider viewModelContextProvider) :
            base("UserLoginIcon", viewModelContextProvider)
        {
            User = user;
            OnSelectUser = ReactiveCommand.Create(SelectUser);
            if (User.UserType == UserType.Render)
            {
                IsRenderUser = true;
            }
        }

        private void SelectUser()
        {
            Selected = true;
        }

        public override void Dispose()
        {
            User = null;
            OnSelectUser?.Dispose();
            base.Dispose();
        }
    }
}
