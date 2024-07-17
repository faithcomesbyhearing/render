using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Models;

public record EditableAudioModel : AudioModel
{
    internal static EditableAudioModel DefaultEmpty()
    {
        return Create(
            data: Array.Empty<byte>());
    }

    public static EditableAudioModel Create(
        string path,
        Guid? key = null)
    {
        return new EditableAudioModel(path, key);
    }

    public static EditableAudioModel Create(
        byte[] data,
        Guid? key = null)
    {
        return new EditableAudioModel(data, key);
    }

    /// <summary>
    /// Start of audio
    /// </summary>
    public double? StartTime { get; set; }

    /// <summary>
    /// End of audio
    /// </summary>
    public double? EndTime { get; set; }

    public double? TotalDuration { get; set; }

    private EditableAudioModel(
        string path,
        Guid? key = null) :
        base(
            path: path,
            name: null,
            startIcon: null,
            endIcon: null,
            isTemp: false,
            isBase: false,
            isLocked: false,
            canDelete: true,
            flags: null,
            key: key,
            option: AudioOption.Optional)
    { }

    private EditableAudioModel(
        byte[] data,
        Guid? key = null) :
        base(
            data: data,
            name: null,
            startIcon: null,
            endIcon: null,
            isTemp: false,
            isBase: false,
            isLocked: false,
            canDelete: true,
            flags: null,
            key: key,
            option: AudioOption.Optional)
    { }
}
