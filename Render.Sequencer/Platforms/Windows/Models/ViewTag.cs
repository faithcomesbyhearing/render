namespace Render.Sequencer.Platforms.Windows.Models;

public record ViewTag
{
    public string Tag { get; }

    public Action Callback { get; }

    public ViewTag(string tag, Action callback)
    {
        Callback = callback;
        Tag = tag;
    }
}