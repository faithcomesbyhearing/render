using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Components.FlagDetail;

public partial class QuestionAndResponse
{
    public QuestionAndResponse()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
			d(this.WhenAnyValue(x => x.ViewModel.QuestionPlayerViewModel)
			   .ObserveOn(RxApp.MainThreadScheduler)
			   .Select(player => player is not null)
			   .Subscribe(hasQuestions =>
			   {
				   QuestionPlayer.IsVisible = hasQuestions;
			   }));
			d(this.OneWayBind(ViewModel, vm => vm.QuestionPlayerViewModel,
                v => v.QuestionPlayer.BindingContext));
			d(this.WhenAnyValue(x => x.ViewModel.ResponsePlayerViewModelList)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(responses =>
                {
                    var source = BindableLayout.GetItemsSource(Responses);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(Responses, responses);
                    }
                }));
        });
    }
}