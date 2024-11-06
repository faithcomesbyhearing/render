using Render.Sequencer.Contracts.Enums;
using Render.Sequencer.Core;
using Render.Sequencer.Core.Utils.Extensions;

namespace Render.Sequencer.Contracts.Models;

public record AudioModel
{
    internal static AudioModel DefaultEmpty(bool isRecorder)
    {
        return Create(
            data: Array.Empty<byte>(),
            name: isRecorder ? InternalRecorder.DefaultRecordName : InternalPlayer.DefaultAudioName);
    }

    public static AudioModel Empty(string? name)
    {
        return new AudioModel(data: Array.Empty<byte>(),
            name: name,
            startIcon: null,
            endIcon: null,
            isTemp: false,
            canDelete: true);
    }

    public static AudioModel Create(
        string path,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        bool isTemp = false,
        bool isBase = false,
        bool isLocked = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional)
    {
        return new AudioModel(path, name, startIcon, endIcon, isTemp, isBase, isLocked, canDelete, flags, key.GetValueOrDefault(), option);
    }

    public static AudioModel Create(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        bool isTemp = false,
        bool isBase = false,
        bool isLocked = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional)
    {
        return new AudioModel(data, name, startIcon, endIcon, isTemp, isBase, isLocked, canDelete, flags, key.GetValueOrDefault(), option);
    }

    /// <summary>
    /// Key of domain audio model
    /// </summary>
    public Guid Key { get; }

    /// <summary>
    /// Usually WaveformItem's label
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// WaveformItem label's start icon. Usually 'PassageNew'. 
    /// </summary>
    public string? StartIcon { get; }

    /// <summary>
    /// WaveformItem label's end icon. Usually 'Checkmark' or 'ReRecord'.
    /// Icon is displayed only in case when AudioOption is Completed.
    /// </summary>
    public string? EndIcon { get; }

    /// <summary>
    /// Audio data
    /// </summary>
    public byte[]? Data { get; }

    /// <summary>
    /// Whether audio has byte data
    /// </summary>
    public bool HasData
    {
        get => !Data.IsNullOrEmpty();
    }

    /// <summary>
    /// Marks current audio as temp. When 'true', WaveForm is not displayed. 
    /// Could be reverted to actual audio. Applicable only in recorder mode.
    /// </summary>
    public bool IsTemp { get; }

    /// <summary>
    /// Local path to temporary audio file
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Used in audio combining mode only.
    /// 'True' means it's center\base audio, around which other audios are combined.
    /// </summary>
    public bool IsBase { get; }

    /// <summary>
    /// Used in audio combining mode only.
    /// 'True' means this audio can't be combined with others until unlocked.
    /// </summary>
    public bool IsInitialyLocked { get; set; }

    /// <summary>
    /// Used in recorder mode.
    /// 'False' means this audio can't be deleted in Recorder mode.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Whether temporary audio file exists
    /// </summary>
    public bool IsFileExists
    {
        get => File.Exists(Path);
    }

    ///Change order
    /// <summary>
    /// Whether audio has byte data or local file path
    /// </summary>
    public bool IsEmpty
    {
        get => !HasData && !IsFileExists;
    }

    /// <summary>
    /// Option to set correct WaveformItem label's ending.
    /// </summary>
    public AudioOption Option { get; }

    /// <summary>
    /// Audio notes, flags.
    /// </summary>
    public List<FlagModel> Flags { get; }
    
    /// <summary>
    /// Ordinal number in sequencer
    /// </summary>
    public string? AudioNumber { get; }

    /// <summary>
    /// Audio samples to draw wave form
    /// </summary>
    public float[]? Samples { get; set; }

    protected AudioModel(
        string path,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        bool isTemp = false,
        bool isBase = false,
        bool isLocked = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional,
        string? number = null)
    {
        Key = key.GetValueOrDefault();
        Path = path;
        Name = name;
        StartIcon = startIcon;
        EndIcon = endIcon;
        IsTemp = isTemp;
        IsBase = isBase;
        IsInitialyLocked = isLocked;
        CanDelete = canDelete;
        Option = option;
        Flags = flags is null ? new List<FlagModel>(0) : new List<FlagModel>(flags);
        AudioNumber = number;
    }

    protected AudioModel(
        byte[] data,
        string? name = null,
        string? startIcon = null,
        string? endIcon = null,
        bool isTemp = false,
        bool isBase = false,
        bool isLocked = false,
        bool canDelete = true,
        IReadOnlyList<FlagModel>? flags = null,
        Guid? key = null,
        AudioOption option = AudioOption.Optional,
        string? number = null)
    {
        Key = key.GetValueOrDefault();
        Data = data;
        Name = name;
        StartIcon = startIcon;
        EndIcon = endIcon;
        IsTemp = isTemp;
        IsBase = isBase;
        IsInitialyLocked = isLocked;
        CanDelete = canDelete;
        Option = option;
        AudioNumber = number;
        Flags = flags is null ? new List<FlagModel>(0) : new List<FlagModel>(flags);
    }
}