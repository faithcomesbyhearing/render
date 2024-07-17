using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Resources;

namespace Render.Components.Consultant;

public partial class SectionSelectCard 
{
    public SectionSelectCard()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, 
                v => v.Chevron.Text, SetChevronDirection));
            d(this.OneWayBind(ViewModel, vm => vm.Section.Title.Text, v => v.SectionTitle.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Section.Number, v => v.SectionNumber.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Section.ScriptureReference, v => v.VerseRange.Text));
            d(this.OneWayBind(ViewModel, vm => vm.Section.Number, v => v.SectionNumber.Text));
            d(this.BindCommandCustom(NavigateToSectionRow, v => v.ViewModel.NavigateToSectionCommand));
            d(this.WhenAnyValue(x => x.FlowDirection)
                .Subscribe(x =>
                {
                    SectionNumber.Margin = x == FlowDirection.RightToLeft ? 
                        new Thickness(0,0) : 
                        new Thickness(7,0 , 0, 0);
                }));
        });
    }
        
    private string SetChevronDirection(FlowDirection flowDirection)
    {
        return flowDirection == FlowDirection.RightToLeft
            ? IconExtensions.GetIconGlyph(Icon.ChevronLeft)
            : IconExtensions.GetIconGlyph(Icon.ChevronRight);
    }
}