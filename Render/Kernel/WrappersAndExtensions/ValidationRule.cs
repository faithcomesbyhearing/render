using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Render.Kernel.WrappersAndExtensions
{
    public class ValidationRule : ReactiveObject
    {
        public string RuleName { get; }
        
        [Reactive]
        public bool IsValid { get; set; }

        public ValidationRule(string ruleName)
        {
            RuleName = ruleName;
        }

        public void SetValidationStatus(bool isValid)
        {
            IsValid = isValid;
        }
    }
}