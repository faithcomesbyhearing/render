using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.Modal;

public partial class Modal
{
    private ModalViewModel ViewModel => (ModalViewModel)BindingContext;

    public Modal()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection,
                v => v.TopLevelElement.FlowDirection));
            d(this.BindCommandCustom(CloseButtonGestureRecognizer, v => v.ViewModel.CloseCommand));
            d(this.BindCommandCustom(CloseModalGestureRecognizer, v => v.ViewModel.CloseCommand));
            d(this.BindCommandCustom(ConfirmGestureRecognizer, v => v.ViewModel.ConfirmCommand));
            d(this.BindCommandCustom(CancelGestureRecognizer, v => v.ViewModel.CancelCommand));
            d(this.OneWayBind(ViewModel, vm => vm.Glyph,
                v => v.Icon.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Title,
                v => v.TitleLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Message,
                v => v.Message.Text));
            d(this.OneWayBind(ViewModel, vm => vm.CancelButtonIsVisible,
                v => v.CancelButton.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.FooterIsVisible,
                v => v.Footer.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.CancelButtonViewModel.Text,
                v => v.CancelButtonLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ConfirmButtonViewModel.Text,
                v => v.ConfirmButtonLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ConfirmButtonViewModel.IsEnabled,
                v => v.ConfirmButton.Opacity, isEnabled => isEnabled ? 1 : 0.3));

            d(this.WhenAnyValue(vm => vm.ViewModel.Glyph).Subscribe(glyph =>
            {
                Icon.IsVisible = string.IsNullOrEmpty(glyph) is false;
            }));


            d(this.WhenAnyValue(vm => vm.ViewModel.ContentViewModel).Subscribe(contentViewModel =>
            {
                if (contentViewModel == null) return;

                var iView = ViewLocator.Current.ResolveView(contentViewModel);

                if (iView is null)
                {
                    throw new ArgumentException($"View can not be found for {contentViewModel.GetType()} View Model");
                }

                iView.ViewModel = contentViewModel;
                var view = (View)iView;
                Message.IsVisible = false;
                MessageWrapper.Children.Add(view);
            }));
        });
    }
}