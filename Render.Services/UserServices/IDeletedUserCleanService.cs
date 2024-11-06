namespace Render.Services.UserServices;

public interface IDeletedUserCleanService
{
    Task Clean(Guid userId);
}