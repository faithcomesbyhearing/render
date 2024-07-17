using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Components.Consultant.ConsultantCheck
{
    public class MenuButtonViewModel : ActionViewModelBase
    {
        public string LabelText { get; }
        public bool IsVisible { get; }
        public string AutomationId { get; }

        [Reactive]
        public bool IsEnabled { get; set; }
        [Reactive]
        public bool IsActive { get; set; }
        [Reactive]
        public bool IsRequired { get; set; }
        public bool IsBackTranslate { get; }
        public bool IsSegmentBackTranslate { get; }
        public bool IsSecondStepBackTranslate { get; }

        public int GridColumn { get; }
        public int GridRow { get; }

        public ParentAudioType AudioType { get; }

        public ReactiveCommand<Unit, bool> BorderTapCommand { get; }

        public MenuButtonViewModel(
            string labelText,
            IViewModelContextProvider viewModelContextProvider,
            MenuButtonParameters parameters) :
            base(ActionState.Optional, "MenuButton", viewModelContextProvider)
        {
            LabelText = labelText;
            AudioType = parameters.AudioType;
            AutomationId = $"{AudioType}";

            IsVisible = parameters.IsVisible;
            IsEnabled = parameters.IsEnabled;

            IsBackTranslate = parameters.IsBackTranslate || parameters.IsSegmentBackTranslate ||
                              parameters.IsSecondStepBackTranslate;
            IsSegmentBackTranslate = parameters.IsSegmentBackTranslate;
            IsSecondStepBackTranslate = parameters.IsSecondStepBackTranslate;

            GridColumn = Convert.ToInt32(IsBackTranslate) + Convert.ToInt32(parameters.IsSegmentBackTranslate);
            GridRow = Convert.ToInt32(parameters.IsSecondStepBackTranslate);

            BorderTapCommand = ReactiveCommand.Create(
                () => IsActive = true,
                this.WhenAnyValue(vm => vm.IsEnabled));
        }
    }
}