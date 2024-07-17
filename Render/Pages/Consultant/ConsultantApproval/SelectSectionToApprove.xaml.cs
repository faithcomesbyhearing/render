using ReactiveUI;
using Render.Extensions;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public partial class SelectSectionToApprove
    {
        public SelectSectionToApprove()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.TopLevelElement.FlowDirection));
                d(this.OneWayBind(ViewModel, vm => vm.TitleBarViewModel, v => v.TitleBar.BindingContext));
                d(this.OneWayBind(ViewModel, vm => vm.IsLoading, v => v.loadingView.IsVisible));
                d(this
                    .WhenAnyValue(x => x.ViewModel.SectionsToApprove.Items)
                    .Subscribe(x =>
                    {
                        var source = BindableLayout.GetItemsSource(ApprovalCardCollection);
                        if (source == null)
                        {
                            BindableLayout.SetItemsSource(ApprovalCardCollection, x);
                        }
                    }));
            });
        }

        protected override void Dispose(bool disposing)
        {
            ApprovalCardCollection
                .Cast<SectionToApproveCard>()
                .ForEach(card => card.Dispose());

            base.Dispose(disposing);
        }
    }
}