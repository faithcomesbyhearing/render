using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace Render.Sequencer.Views.Toolbar.ToolbarItems;

public partial class ToolbarItemView : ContentView
{
	private IDisposable? _clickSubscription;

    public BaseToolbarItemViewModel? ViewModel 
	{ 
		get => BindingContext as BaseToolbarItemViewModel;
	}

	public ToolbarItemView()
	{
		InitializeComponent();
	}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

		_clickSubscription?.Dispose();
		
		if (ViewModel is not null)
		{
			_clickSubscription = Observable
				.FromEventPattern(actionButton, nameof(Button.Clicked))
				.Throttle(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
				.Take(1)
				.Concat(Observable
					.Empty<EventPattern<object>>()
					.Delay(TimeSpan.FromMilliseconds(400), RxApp.MainThreadScheduler))
				.Repeat()
				.Select(_ => Unit.Default)
				.InvokeCommand(ViewModel.ActionCommand);
		}
    }
}