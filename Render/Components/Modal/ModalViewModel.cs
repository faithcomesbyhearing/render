using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.Modal;

public class ModalViewModel : ViewModelBase
{
    [Reactive]
    public string Glyph { get; set; }
    public string Title { get; }
    public string Message { get; }

    private readonly IModalService _modalService;

    public ModalButtonViewModel ConfirmButtonViewModel { get; protected set; }
    public ModalButtonViewModel CancelButtonViewModel { get; protected set; }

    [Reactive] public ViewModelBase ContentViewModel { get; set; }
    
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    [Reactive] public ReactiveCommand<Unit, bool> BeforeConfirmCommand { get; set; }
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    [Reactive] public ReactiveCommand<Unit, Unit> AfterConfirmCommand { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public bool CancelButtonIsVisible { get; set; }

    public bool FooterIsVisible { get; init; } = true;

    private readonly Func<Task> _onClose;

    public ModalViewModel(
        IViewModelContextProvider viewModelContextProvider,
        IModalService modalService,
        Icon? icon,
        string title,
        string message,
        ModalButtonViewModel confirmButtonViewModel,
        Func<Task> onClose = null) : base(nameof(ModalViewModel), viewModelContextProvider)
    {
        _modalService = modalService;
        Glyph = icon is not null ? IconExtensions.BuildFontImageSource((Icon)icon).Glyph : null;
        Title = title;
        Message = message;
        ConfirmButtonViewModel = confirmButtonViewModel;
        _onClose = onClose;
        CloseCommand = ReactiveCommand.CreateFromTask(() => OnClosed(DialogResult.None));
        ConfirmCommand = ReactiveCommand.Create(OnConfirmPressed);
    }

    public ModalViewModel(
        IViewModelContextProvider viewModelContextProvider,
        IModalService modalService,
        Icon? icon,
        string title,
        string message,
        ModalButtonViewModel cancelButtonViewModel,
        ModalButtonViewModel confirmButtonViewModel)
        : this(viewModelContextProvider, modalService, icon, title, message, confirmButtonViewModel)
    {
        CancelButtonViewModel = cancelButtonViewModel;
        CancelCommand = ReactiveCommand.CreateFromTask(() => OnClosed(DialogResult.Cancel));
        CancelButtonIsVisible = true;
    }

    public ModalViewModel(
        IViewModelContextProvider viewModelContextProvider,
        IModalService modalService,
        Icon? icon,
        string title,
        ViewModelBase contentContentViewModel,
        ModalButtonViewModel cancelButtonViewModel,
        ModalButtonViewModel confirmButtonViewModel)
        : this(viewModelContextProvider, modalService, icon, title, null, confirmButtonViewModel)
    {
            ContentViewModel = contentContentViewModel;
            CancelButtonViewModel = cancelButtonViewModel;
            CancelCommand = ReactiveCommand.CreateFromTask(() => OnClosed(DialogResult.Cancel));
            CancelButtonIsVisible = true;

            Disposables.Add(this.WhenAnyValue(x => x.BeforeConfirmCommand)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(command =>
                {
                    if (command != null)
                    {
                        Disposables.Add(command.CanExecute
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .Subscribe(confirmEnabled => { ConfirmButtonViewModel.IsEnabled = confirmEnabled; }));
                    }
                }));
    }
    
    private void OnConfirmPressed()
    {
        if (!ConfirmButtonViewModel.IsEnabled) return;

        if (BeforeConfirmCommand != null)
        {
            BeforeConfirmCommand.Execute().Subscribe(success =>
            {
                if (success)
                {
                    _modalService.Close(DialogResult.Ok);

                    AfterConfirmCommand?.Execute().Subscribe();
                }
            });
        }

        else if (AfterConfirmCommand != null)
        {
            _modalService.Close(DialogResult.Ok);
            AfterConfirmCommand.Execute().Subscribe();
        }
        
        else
        {
            _modalService.Close(DialogResult.Ok);
        }
    }
    
    private async Task OnClosed(DialogResult dialogResult)
    {
        _modalService.Close(dialogResult);
        if (_onClose != null)
        {
            await _onClose.Invoke();
        }
    }
    
    public override void Dispose()
    {
        CloseCommand?.Dispose();
        BeforeConfirmCommand?.Dispose();
        ConfirmCommand?.Dispose();
        AfterConfirmCommand?.Dispose();
        CancelCommand?.Dispose();
        
        base.Dispose();
    }
}