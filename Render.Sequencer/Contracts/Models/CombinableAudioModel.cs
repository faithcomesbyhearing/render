using Render.Sequencer.Contracts.Enums;

namespace Render.Sequencer.Contracts.Models;

public record CombinableAudioModel : AudioModel
{
    public static new CombinableAudioModel Empty(string? name)
    {
        return new CombinableAudioModel(Array.Empty<byte>(), name);
    }

    public static CombinableAudioModel Create(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        bool isBase=false,
        bool isLocked=false,
        Guid? key = null,
        string? number = null)
    {
        return new CombinableAudioModel(data, name, startIcon, isBase, isLocked, key, number);
    }

    public static CombinableAudioModel Create(
        string path,
        string? name = null,
        string? startIcon = null,
        bool isBase=false,
        bool isLocked=false,
        Guid? key = null,
        string? number = null)
    {
        return new CombinableAudioModel(path, name, startIcon, isBase, isLocked, key, number);
    }

    private CombinableAudioModel(
        string path,
        string? name = null,
        string? startIcon = null,
        bool isBase=false,
        bool isLocked=false,
        Guid? key = null,
        string? number = null) : base(
            path: path,
            name: name,
            startIcon: startIcon,
            endIcon: null,
            isTemp: false,
            isBase: isBase,
            isLocked: isLocked,
            canDelete: false,
            flags: null,
            key: key,
            option: AudioOption.Optional,
            number: number)
    { }

    private CombinableAudioModel(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        bool isBase=false,
        bool isLocked=false,
        Guid? key = null, string? number = null) : base(
            data: data,
            name: name,
            startIcon: startIcon,
            endIcon: null,
            isTemp: false,
            isBase: isBase,
            isLocked: isLocked,
            canDelete: false,
            flags: null,
            key: key,
            option: AudioOption.Optional,
            number: number)
    { }
}