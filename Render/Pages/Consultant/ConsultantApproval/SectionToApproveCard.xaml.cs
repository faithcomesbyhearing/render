using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Pages.Consultant.ConsultantApproval
{
    public partial class SectionToApproveCard 
    {
        public SectionToApproveCard()
        {
            InitializeComponent();

            DisposableBindings = this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Section.Title.Text, v => v.SectionTitle.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Section.Number, v => v.SectionNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Section.ScriptureReference, v => v.VerseRange.Text));
                d(this.BindCommandCustom(NavigateToSectionButtonGestureRecognizer, v => v.ViewModel.NavigateToSectionCommand));
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.Chevron.Text, SetChevronDirection));
                d(this
                    .WhenAnyValue(x => x.FlowDirection)
                    .Subscribe(x =>
                    {
                        SectionNumber.Margin = x == FlowDirection.RightToLeft ? 
                            new Thickness(0, 5, 0, 0) : 
                            new Thickness(7, 5 , 0, 0);
                    }));
            });
        }
        
        private string SetChevronDirection(FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft ? 
                IconExtensions.GetIconGlyph(Icon.ChevronLeft) : 
                IconExtensions.GetIconGlyph(Icon.ChevronRight);
        }
    }
}