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

        public int GridColumn { get; }
        public int GridRow { get; }

        public ParentAudioType AudioType { get; }
        public MenuTabType MenuTabType { get; }

        public ReactiveCommand<Unit, bool> BorderTapCommand { get; }

        public MenuButtonViewModel(
            string labelText,
            IViewModelContextProvider viewModelContextProvider,
            MenuButtonParameters parameters) :
            base(ActionState.Optional, "MenuButton", viewModelContextProvider)
        {
            LabelText = labelText;
            AudioType = parameters.AudioType;
            MenuTabType = parameters.MenuTabType;
            AutomationId = $"{AudioType}";

            IsVisible = parameters.IsVisible;
            IsEnabled = parameters.IsEnabled;

            GridColumn = GetGridColumnNumber(MenuTabType);
            GridRow = GetGridRowNumber(MenuTabType);

            BorderTapCommand = ReactiveCommand.Create(
                () => IsActive = true,
                this.WhenAnyValue(vm => vm.IsEnabled));
        }

        private int GetGridColumnNumber(MenuTabType menuTabType)
        {
            return menuTabType switch
            {
                MenuTabType.BackTranslate => 1,
                MenuTabType.BackTranslate2 => 1,
                MenuTabType.SegmentBackTranslate => 2,
                MenuTabType.SegmentBackTranslate2 => 2,
                _ => 0
            };
        }

        private int GetGridRowNumber(MenuTabType menuTabType)
        {
            return MenuTabType switch
            {
                MenuTabType.BackTranslate2 => 1,
                MenuTabType.SegmentBackTranslate2 => 1,
                _ => 0
            };
        }
    }
}