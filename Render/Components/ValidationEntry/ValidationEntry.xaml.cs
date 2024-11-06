using System.Reactive.Linq;
using ReactiveUI;
using Render.Repositories.Extensions;

namespace Render.Components.ValidationEntry;

public partial class ValidationEntry
{
    public ValidationEntry()
    {
        InitializeComponent();

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.Bind(ViewModel, vm => vm.Value, v => v.ValueEntry.Text));
            d(this.OneWayBind(ViewModel, vm => vm.ValidationMessage, v => v.ValidationLabel.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Label, v => v.ValueLabel.Text));
            d(this.BindCommand(ViewModel, vm => vm.OnEnterCommand, v => v.ValueEntry,
                nameof(Entry.Completed)));
            d(this.OneWayBind(ViewModel, vm => vm.IsPassword,
                v => v.ValueEntry.IsPassword));
            d(this.OneWayBind(ViewModel, vm => vm.MaxLength,
                v => v.ValueEntry.MaxLength));
            d(this.BindCommand(ViewModel, vm => vm.OnTogglePasswordCommand,
                v => v.ShowPasswordTap));
            d(this.OneWayBind(ViewModel, vm => vm.ShowTogglePasswordButton,
                v => v.ShowPassword.IsVisible));
            d(this.OneWayBind(ViewModel, vm => vm.PlaceholderText,
                v => v.ValueEntry.Placeholder));
            d(this.WhenAnyValue(x => x.ViewModel.InFocus)
                .Subscribe(arg =>
                {
                    if (arg)
                    {
                        ValueEntry.Focus();
                    }
                }));
            
            d(this.WhenAnyValue(x => x.ViewModel.Value)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(value =>
                {
                    ValueEntry.FontFamily = value.IsNullOrEmpty() ? "RegularItalicFont" : "RegularFont";
                }));
        });
    }
}