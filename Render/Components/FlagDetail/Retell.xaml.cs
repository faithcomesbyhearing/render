using ReactiveUI;

namespace Render.Components.FlagDetail;

public partial class Retell
{
    public Retell()
    {
        InitializeComponent();
            
        this.WhenActivated(d =>
        {
            d(this.OneWayBind(ViewModel, vm => vm.RetellPlayerViewModel, 
                v => v.RetellPlayer.BindingContext));
        });
    }
}