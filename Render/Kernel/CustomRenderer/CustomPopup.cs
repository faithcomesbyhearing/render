using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace Render.Kernel.CustomRenderer
{
    public class CustomPopup : Popup
    {
        private static readonly Stack<CustomPopup> PopupStack = new ();

        public CustomPopup()
        {
            Opened += CustomPopup_Opened;
        }

        public override View Content
        {
            set
            {
                var layout = value.GetVisualTreeDescendants().FirstOrDefault(child => child is IBindableLayout);
                if (layout is null)
                {
                    throw new ArgumentException("Popup content must have any layout element");
                }

                var entry = new Entry
                {
                    AutomationId = "HiddenEntry",
                    WidthRequest = 0,
                    HeightRequest = 0
                };
                entry.Focused += Entry_Focused;
                ((IBindableLayout)layout).Children.Add(entry);

                base.Content = value;
            }
        }

        protected override Task OnClosed(
            object result,
            bool wasDismissedByTappingOutsideOfPopup,
            CancellationToken token)
        {
            if (PopupStack.TryPeek(out var storedPopup)
                && this == storedPopup)
            {
                PopupStack.Pop();
                Opened -= CustomPopup_Opened;
                if (PopupStack.TryPeek(out var previousPopup))
                {
                    previousPopup?.Content?.Focus();
                }
            }

            return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
        }

        private void CustomPopup_Opened(object sender, PopupOpenedEventArgs e)
        {
            if (sender is CustomPopup popup)
            {
                PopupStack.Push(popup);
                popup.Content?.Focus();
            }
        }

        private static async void Entry_Focused(object sender, EventArgs e)
        {
            if (sender is ITextInput textInput
                && textInput.IsSoftKeyboardShowing())
            {
                await textInput.HideKeyboardAsync(CancellationToken.None);
            }
        }
    }
}
