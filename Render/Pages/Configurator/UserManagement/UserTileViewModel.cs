using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Users;
using Render.Pages.Settings;
using Render.Pages.Settings.ManageUsers;
using Render.Resources.Localization;
using Render.TempFromVessel.Project;
using Render.TempFromVessel.User;

namespace Render.Pages.Configurator.UserManagement;

 public class UserTileViewModel : ViewModelBase
    {
        [Reactive]
        public string FullName { get; set; }
        public UserType UserType { get; }
        
        [Reactive]
        public ImageSource UserIconSource { get; private set; }
        
        public string Roles { get;  set; }

        public bool HasRoles => Roles.Length > 1;
        
        public ReactiveCommand<Unit, IRoutableViewModel> EditUserCommand { get; }
        
        public Guid UserId { get; private set;}
        
        private IUser _user;
        private Project _project;

        public UserTileViewModel(IViewModelContextProvider viewModelContextProvider, IUser user, Project project)
            : base ("UserTile", viewModelContextProvider)
        {
            _user = user;
            UserId = _user.Id;
            _project = project;
            UserType = _user.UserType;
            SetupUser();
            
            EditUserCommand = ReactiveCommand.CreateFromTask(NavigateToEditUserAsync);
        }

        public void UpdateUser(IUser user)
        {
            if(user.Id != _user.Id)
                return;
            _user = user;
            SetupUser();
        }

        private void SetupUser()
        {
            FullName = _user.FullName;
            if (_user.UserIcon.Length > 0)
            {
                var imageStream = new MemoryStream(_user.UserIcon);
                UserIconSource = ImageSource.FromStream(() => imageStream);
            }
            GetUserRoles();
        }

        private void GetUserRoles()
        {
            Roles = "";
            var roles = _user.RolesForProject(RenderRolesAndClaims.ProjectUserClaimType, _project.Id.ToString());
            foreach (var role in roles.Where(role => role.Name != RoleName.General))
            {
                switch (role.Name)
                {
                    case RoleName.Configure:
                        Roles += AppResources.Configure + ", ";
                        break;
                    case RoleName.Consultant:
                        Roles += AppResources.Consultant + ", ";
                        break;
                }
            }
            //remove trailing comma and space
            if (Roles.Length > 1)
            {
                Roles = Roles.Substring(0, Roles.Length - 2);
            }
        }

        private async Task<IRoutableViewModel> NavigateToEditUserAsync() 
        {
            var editUserViewModel = await UserSettingsViewModel.CreateAsync(ViewModelContextProvider, _project.Id, false, _user);
            return await NavigateTo(editUserViewModel);
        }
    }