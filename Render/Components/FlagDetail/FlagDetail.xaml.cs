using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Components.FlagDetail;

public partial class FlagDetail
{
    public FlagDetail()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.Glyph, v => v.Icon.Text));
            d(this.BindCommandCustom(CloseGesture, v => v.ViewModel.CloseModalCommand));
            d(this.BindCommandCustom(BackgroundGesture, v => v.ViewModel.CloseModalCommand));
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.ComponentLayout.FlowDirection));

            d(this.WhenAnyValue(x => x.ViewModel.QuestionAndResponses)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(questions =>
                {
                    var source = BindableLayout.GetItemsSource(QuestionList);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(QuestionList, questions);
                    }
                }));

            d(this.WhenAnyValue(x => x.ViewModel.QuestionAndResponses.Count)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(count => count > 0)
                .Subscribe(hasQuestions =>
                {
                    NoQuestionLabel.IsVisible = !hasQuestions;
                    QuestionList.IsVisible = hasQuestions;
                }));

            d(this.WhenAnyValue(x => x.ViewModel.Retells.Count)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(count => count > 0)
                .Subscribe(hasRetells =>
                {
                    NoRetellLabel.IsVisible = !hasRetells;
                    RetellList.IsVisible = hasRetells;
                }));

            d(this.WhenAnyValue(x => x.ViewModel.Retells)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(retells =>
                {
                    var source = BindableLayout.GetItemsSource(RetellList);
                    if (source == null)
                    {
                        BindableLayout.SetItemsSource(RetellList, retells);
                    }
                }));

            d(this.OneWayBind(ViewModel, vm => vm.FlagDetailNavigationViewModel,
                v => v.NavigationView.BindingContext));
            d(this.WhenAnyValue(v => v.ViewModel.FlagDetailNavigationViewModel)
                .Subscribe(vm => NavigationView.IsVisible = vm != null));

            d(this.WhenAnyValue(x => x.ViewModel.IsLoading)
                .Subscribe(loading =>
                {
                    LoadingView.IsVisible = loading;
                }));
        });
    }
}