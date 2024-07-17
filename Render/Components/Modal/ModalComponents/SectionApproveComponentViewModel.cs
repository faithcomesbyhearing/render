using System.Reactive;
using Microsoft.AspNetCore.Identity;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.ValidationEntry;
using Render.Kernel;
using Render.Models.Users;
using Render.Repositories.Extensions;
using Render.Resources.Localization;
using Render.Services.PasswordServices;

namespace Render.Components.Modal.ModalComponents;

public class SectionApproveComponentViewModel : ValidationEntryViewModel
{
    private readonly IUser _user;

    [Reactive] private bool ValidPassword { get; set; }
    [Reactive] public bool AllowConfirmCommand { get; private set; }

    public ReactiveCommand<Unit, bool> ConfirmCommand { get; }

    private readonly Func<Task> _confirmCallback;

    public SectionApproveComponentViewModel(
        IViewModelContextProvider viewModelContextProvider,
        Func<Task> sectionApproveCallBack = null)
        : base(string.Empty, viewModelContextProvider, true, AppResources.EnterYourPassword)
    {
        _user = viewModelContextProvider.GetLoggedInUser();
        _confirmCallback = sectionApproveCallBack;

        ConfirmCommand = ReactiveCommand.CreateFromTask(Confirm, this.WhenAnyValue(x => x.AllowConfirmCommand));

        Disposables.Add(this
            .WhenAnyValue(x => x.Value)
            .Subscribe(v =>
            {
                AllowConfirmCommand = !v.IsNullOrEmpty();
                SetValidation(string.Empty);
            }));
    }

    private async Task<bool> Confirm()
    {
        TryValidate();
        if (!ValidPassword)
        {
            return false;
        }

        if (_confirmCallback != null)
        {
            await _confirmCallback.Invoke();
            return false;
        }

        return true;
    }

    private void TryValidate()
    {
        var vesselPasswordValidator = new VesselPasswordValidator();

        if (_user == null) return;

        if (string.IsNullOrWhiteSpace(Value))
        {
            SetValidation(AppResources.EmptyPassword);
        }
        else
        {
            var result = vesselPasswordValidator.ValidatePassword(_user as User, Value);
            ValidPassword = result != PasswordVerificationResult.Failed;
            if (ValidPassword)
            {
                LogInfo("Section UnApproved", new Dictionary<string, string> {{"Consultant Name", _user.Username}});
            }
            else
            {
                SetValidation(AppResources.WrongPassword);
                LogInfo("Approve failed: incorrect password", new Dictionary<string, string> {{"User", _user.Username}});
            }
        }
    }
}