using ReactiveUI;

namespace Render.Components.TitleBar.MenuActions
{
    public partial class MenuAction
    {
        public MenuAction()
        {
            InitializeComponent();
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Label.Text));
                d(this.OneWayBind(ViewModel, vm => vm.Glyph, v => v.Image.Text));
                d(this.BindCommand(ViewModel, vm => vm.Command, v => v.GestureRecognizer));
            });
        }
    }
}