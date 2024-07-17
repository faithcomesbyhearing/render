using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Models;

public record PlayerAudioModel : AudioModel
{
    public static new PlayerAudioModel Empty(string? name)
    {
        return new PlayerAudioModel(Array.Empty<byte>(), name);
    }

    public static PlayerAudioModel Create(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional)
    {
        return new PlayerAudioModel(data, name, startIcon, endIcon, flags, key, option);
    }

    public static PlayerAudioModel Create(
        string path,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional,
        string? number = null)
    {
        return new PlayerAudioModel(path, name, startIcon, endIcon, flags, key, option, number);
    }

    private PlayerAudioModel(
        string path,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional,
        string? number = null) :
        base(path: path,
             name: name,
             startIcon: startIcon,
             endIcon: endIcon,
             isTemp: false,
             isBase: false,
             isLocked: false,
             canDelete: false,
             flags: flags,
             key: key,
             option: option,
             number: number)
    { }

    private PlayerAudioModel(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional) :
        base(
            data: data,
            name: name,
            startIcon: startIcon,
            endIcon: endIcon,
            isTemp: false,
            isBase: false,
            isLocked: false,
            canDelete: false,
            flags: flags,
            key: key,
            option: option)
    { }
}