using System.Reactive.Linq;
using ReactiveUI;
using Render.Kernel;
using Render.Resources;

namespace Render.Components.DraftSelection
{
    public partial class DraftSelection
    {
        public const double OuterCircleSize = 26;
        public const double MiddleCircleSize = 20;
        public const double InnerCircleSize = 12;
        
        public DraftSelection()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.MiniWaveformPlayerViewModel, v => v.MiniWaveformPlayer.BindingContext));
                d(this.BindCommand(ViewModel, vm => vm.SelectCommand, v => v.FrameTapped));

                d(this.WhenAnyValue(p => p.ViewModel.DraftSelectionState)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(SetState));
            });
        }

        private void SetState(DraftSelectionState state)
        {
            Color innerCircleColor = Colors.Transparent;
            Color middleCircleColor = ResourceExtensions.GetColor("SecondaryText");
            Color outerCircleColor = Colors.Transparent;

            Color backgroundColor = Colors.Transparent;

            if (state == DraftSelectionState.Required)
            {
                innerCircleColor = ResourceExtensions.GetColor("SecondaryText");
                outerCircleColor = ResourceExtensions.GetColor("DraftSelectionOuterCircleRequired");

                backgroundColor = ResourceExtensions.GetColor("Required");
            }

            if (state == DraftSelectionState.Selected)
            {
                innerCircleColor = ResourceExtensions.GetColor("Option");
                outerCircleColor = ResourceExtensions.GetColor("SecondaryText");

                backgroundColor = ResourceExtensions.GetColor("Option");
            }

            if (state == DraftSelectionState.Unselected)
            {
                innerCircleColor = ResourceExtensions.GetColor("SecondaryText");
                outerCircleColor = ResourceExtensions.GetColor("Option");

                backgroundColor = ResourceExtensions.GetColor("DraftSelectionBackgroundUnselected");
            }

            UpdateColorsBasedOnState(backgroundColor, outerCircleColor, middleCircleColor, innerCircleColor);

            if (ViewModel != null && ViewModel.MiniWaveformPlayerViewModel.ActionState != ActionState.Inactive)
            {
                ViewModel.MiniWaveformPlayerViewModel.ActionState = state == DraftSelectionState.Required ? ActionState.Required : ActionState.Optional;
            }
        }

        private void UpdateColorsBasedOnState(
            Color backgroundColor,
            Color outerCircleColor,
            Color middleCircleColor,
            Color innerCircleColor)
        {
            InnerCircle.SetValue(BackgroundColorProperty, innerCircleColor);
            MiddleCircle.SetValue(BackgroundColorProperty, middleCircleColor);
            OuterCircle.SetValue(BackgroundColorProperty, outerCircleColor);

            BackgroundContainer.SetValue(BackgroundColorProperty, backgroundColor);
        }
    }
}