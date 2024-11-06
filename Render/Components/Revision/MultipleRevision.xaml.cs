using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources.Localization;
using System.Reactive.Linq;

namespace Render.Components.Revision;

public partial class MultipleRevision
{
	public MultipleRevision()
    {
        InitializeComponent();

        CurrentRevisionLabel.Text = string.Format(AppResources.Current, string.Empty);

        DisposableBindings = this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.RevisionItems, v => v.RevisionPicker.ItemsSource));
            d(this.Bind(ViewModel, vm => vm.SelectedRevisionItem, v => v.RevisionPicker.SelectedItem));

            d(this.WhenAnyValue(x => x.ViewModel.RevisionItems.Count)
                .Where(count => count is not 0)
                .Subscribe(count =>
                {
                    RevisionLayout.SetValue(IsVisibleProperty, count > 1);
                }));

            d(this.WhenAnyValue(x => x.ViewModel.IsCurrentRevision)
                .Subscribe(isCurrentRevision =>
                {
                    CurrentRevisionButton.SetValue(IsVisibleProperty, !isCurrentRevision);
                }));

            d(this.BindCommandCustom(CurrentRevisionButtonTap, v => v.ViewModel.SelectCurrentRevisionCommand));
        });
    }
}