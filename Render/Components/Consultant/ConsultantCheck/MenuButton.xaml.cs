using ReactiveUI;
using Render.Kernel;
using Render.Resources;

namespace Render.Components.Consultant.ConsultantCheck
{
    public partial class MenuButton
    {
        private readonly Color _optionalColor;
        private readonly Color _secondaryColor;
        private readonly Color _transparentColor;
        private readonly Color _requiredColor;
        
        public MenuButton()
        {
            InitializeComponent();
            
            _optionalColor = ResourceExtensions.GetColor("Option");
            _secondaryColor = ResourceExtensions.GetColor("SecondaryText");
            _transparentColor = ResourceExtensions.GetColor("Transparent");
            _requiredColor = ResourceExtensions.GetColor("ConsultantCheckRequiredNotes");
            
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.LabelText, v => v.Label.Text));
                d(this.BindCommand(ViewModel, vm => vm.BorderTapCommand, v => v.BorderTap));
                d(this.WhenAnyValue(
                        menuButton => menuButton.ViewModel.IsActive,
                        menuButton => menuButton.ViewModel.IsRequired,
                        menuButton => menuButton.ViewModel.ActionState)
                    .Subscribe(((bool IsActive, bool IsRequired, ActionState ActionState) properties) =>
                    {
                        ChangeStyle(properties.IsActive, properties.IsRequired, properties.ActionState);
                    }));
                d(this.OneWayBind(ViewModel, vm => vm.IsVisible, v => v.IsVisible));
                d(this.WhenAnyValue(x => x.ViewModel.IsEnabled).Subscribe(ChangeOpacity));
            });
        }

        private void ChangeOpacity(bool isEnabled)
        {
            Border.SetValue(IsEnabledProperty, isEnabled);
            Border.SetValue(OpacityProperty, isEnabled ? 1 : 0.3);
        }

        private void ChangeStyle(bool isActive, bool isRequired, ActionState actionState)
        {
            Border.SetValue(BackgroundColorProperty, isActive ? _optionalColor : _transparentColor);
            var labelTextColor = isRequired && actionState == ActionState.Required
                ? _requiredColor :
                isActive ?
                    _secondaryColor :
                    _optionalColor;
            Label.SetValue(Label.TextColorProperty, labelTextColor);
        }
    }
}