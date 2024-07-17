using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using Render.Kernel;
using System.Reactive;
using Render.Repositories.Extensions;

namespace Render.Components.ValidationEntry
{
    public class ValidationEntryViewModel : ViewModelBase
    {
        [Reactive] public string Value { get; set; }
        [Reactive] public bool IsValid { get; set; }
        [Reactive] public string ValidationMessage { get; set; }
        public string Label { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> OnEnterCommand { get; set; }
        [Reactive] public bool IsPassword { get; set; }
        [Reactive] public bool ShowTogglePasswordButton { get; private set; }
        
        public string PlaceholderText;
        public ReactiveCommand<Unit, Unit> OnTogglePasswordCommand { get; set; }
        [Reactive] public bool InFocus { get; set; }

        public ValidationEntryViewModel(
            string label,
            IViewModelContextProvider viewModelContextProvider,
            bool isPassword,
            string placeholderText)
            : base("ValidationEntry", viewModelContextProvider)
        {
            Label = label;
            PlaceholderText = placeholderText;
            Disposables.Add(this.WhenAnyValue(x => x.ValidationMessage)
                .Subscribe(s =>
                {
                    IsValid = string.IsNullOrEmpty(s);
                    if (!IsValid)
                    {
                        PutEntryInFocus(true);
                    }
                }));

            Disposables.Add(this.WhenAnyValue(x => x.Value)
                .Subscribe(x =>
            {
                ShowTogglePasswordButton = !x.IsNullOrEmpty() && isPassword;
                ClearValidation();
            }));
            
            if (isPassword)
            {
                IsPassword = true;
                OnTogglePasswordCommand = ReactiveCommand.Create(ToggleShowPassword);
            }

            OnEnterCommand = ReactiveCommand.Create(() => { });
        }

        public bool CheckValidation()
        {
            return IsValid;
        }

        public void SetValidation(string message)
        {
            ValidationMessage = message;
        }

        public void ClearValidation()
        {
            ValidationMessage = "";
        }

        private void ToggleShowPassword()
        {
            IsPassword = !IsPassword;
        }

        public void PutEntryInFocus(bool inFocus)
        {
            InFocus = inFocus;
        }
    }
}