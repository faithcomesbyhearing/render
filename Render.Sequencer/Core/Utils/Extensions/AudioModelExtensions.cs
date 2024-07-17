using Render.Sequencer.Contracts.Models;

namespace Render.Sequencer.Core.Utils.Extensions;

public static class AudioModelExtensions
{
    public static string GetFullName(this AudioModel model)
    {
        return string.IsNullOrEmpty(model.AudioNumber) ? 
            model.Name ?? string.Empty : 
            $"{model.Name} {model.AudioNumber}";
    }
}