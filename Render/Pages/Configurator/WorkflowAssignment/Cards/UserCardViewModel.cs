using Render.Kernel;
using Render.Models.Users;

namespace Render.Pages.Configurator.WorkflowAssignment.Cards;

public class UserCardViewModel : ViewModelBase
{
    public IUser User { get; }
    public string Name { get; }
    public string FullName { get; }
    
    public UserCardViewModel(IUser user, IViewModelContextProvider viewModelContextProvider) :
        base("UserCardViewModel", viewModelContextProvider)
    {
        
        User = user;
        Name = user.Username;
        FullName = user.FullName;
    }
}