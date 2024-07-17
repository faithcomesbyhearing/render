using ReactiveUI;
using Render.Models.Users;
using System.Reactive;

namespace Render.Components.ProfileAvatar
{
    public interface IUserLoginIconViewModel : IDisposable
    {
        IUser User { get; }
        bool Selected { get; set; }
        bool IsRenderUser { get; }
        ReactiveCommand<Unit, Unit> OnSelectUser { get; }
    }
}
