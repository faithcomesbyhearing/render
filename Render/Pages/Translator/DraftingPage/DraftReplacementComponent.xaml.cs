using System.Reactive.Linq;
using ReactiveUI;

namespace Render.Pages.Translator.DraftingPage
{
    public partial class DraftReplacementComponent
    {
        public DraftReplacementComponent()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                    v => v.TopLevelElement.FlowDirection));
                d(this.WhenAnyValue(x => x.ViewModel.DraftCards.SourceItems)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(cards =>
                    {
                        var source = BindableLayout.GetItemsSource(TopLevelElement);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(TopLevelElement, cards);
                        }
                    })
               );
            });
        }
    }
}