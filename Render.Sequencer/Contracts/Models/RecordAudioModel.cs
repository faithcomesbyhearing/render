namespace Render.Sequencer.Contracts.Models;

public record RecordAudioModel : AudioModel
{
    public static new RecordAudioModel Empty(string? name=null)
    {
        return new RecordAudioModel(
            data: Array.Empty<byte>(),
            name: name,
            isTemp: false,
            canDelete: true);
    }

    public static RecordAudioModel Create(
        string path,
        string? name = null,
        bool isTemp = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null)
    {
        return new RecordAudioModel(path, name, isTemp, canDelete, flags);
    }

    public static RecordAudioModel Create(
        byte[] data,
        string? name = null,
        bool isTemp = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null)
    {
        return new RecordAudioModel(data, name, isTemp, canDelete, flags);
    }

    private RecordAudioModel(
        string path,
        string? name = null,
        bool isTemp = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null) : 
        base(
            path: path,
            name: name,
            startIcon: null,
            endIcon: null,
            isTemp: isTemp,
            isBase: false,
            isLocked: false,
            canDelete: canDelete,
            flags: flags) 
    { }

    private RecordAudioModel(
        byte[] data,
        string? name = null,
        bool isTemp = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null) : 
        base(
            data: data,
            name: name,
            startIcon: null,
            endIcon: null,
            isTemp: isTemp,
            isBase: false,
            isLocked: false,
            canDelete: canDelete,
            flags: flags) 
    { }
}