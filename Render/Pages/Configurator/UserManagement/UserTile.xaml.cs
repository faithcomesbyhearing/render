using ReactiveUI;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Users;
using Render.Resources;
using Render.Resources.Localization;
using Render.Resources.Styles;

namespace Render.Pages.Configurator.UserManagement;

  public partial class UserTile
    {
        public UserTile()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.FlowDirection, v => v.Chevron.Text, SetBackButtonDirection));
                d(this.OneWayBind(ViewModel, vm => vm.FullName, v => v.UserFullName.Text));
                d(this.OneWayBind(ViewModel, vm => vm.UserType, v => v.UserTypeLabel.Text, 
                    ConversionHint));
                d(this.OneWayBind(ViewModel, vm => vm.Roles, v => v.UserRoles.Text));
                d(this.OneWayBind(ViewModel, vm => vm.HasRoles, v => v.UserRoles.IsVisible));
                d(this.OneWayBind(ViewModel, vm => vm.UserType, v => v.UserTypeLabel.TextColor, ConversionHintTypeTextColor));
                d(this.OneWayBind(ViewModel, vm => vm.UserType, v => v.UserTypeFrame.BackgroundColor,
                    ConversionHintFrameBackground));
                d(this.BindCommandCustom(TapGestureRecognizer, v => v.ViewModel.EditUserCommand));
            });
        }
        
        private string SetBackButtonDirection(FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.RightToLeft
                ? IconExtensions.GetIconGlyph(Icon.ChevronLeft)
                : IconExtensions.GetIconGlyph(Icon.ChevronRight);
        }

        private Color ConversionHintTypeTextColor(UserType arg)
        {
            switch (arg)
            {
                case UserType.Render:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserTypeText")).Color;
                case UserType.Vessel:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserTypeText")).Color;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private Color ConversionHintFrameBackground(UserType arg)
        {
            switch (arg)
            {
                case UserType.Render:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("RenderUserType")).Color;
                case UserType.Vessel:
                    return ((ColorReference)ResourceExtensions.GetResourceValue("VesselUserType")).Color;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private string ConversionHint(UserType arg)
        {
            switch (arg)
            {
                case UserType.Render:
                    return AppResources.Render.ToUpper();
                case UserType.Vessel:
                    return AppResources.Global.ToUpper();
            }

            return "";
        }
        
    }