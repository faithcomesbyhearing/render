using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Resources;

namespace Render.Components.Modal;

public class ModalButtonViewModel : ReactiveObject
{
    public Icon Glyph { get; set; }

    public string Text { get; set; }

    [Reactive] public bool IsEnabled { get; set; }

    public ModalButtonViewModel(string text)
    {
        Text = text;
        IsEnabled = true;
    }

}