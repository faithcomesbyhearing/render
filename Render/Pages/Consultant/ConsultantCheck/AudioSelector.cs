using Render.Extensions;
using Render.Kernel;
using Render.Models.Sections;
using Render.Sequencer.Contracts.Models;

namespace Render.Pages.Consultant.ConsultantCheck;

public class AudioSelector
{
    private readonly IViewModelContextProvider _viewModelContextProvider;

    public AudioSelector(IViewModelContextProvider viewModelContextProvider)
    {
        _viewModelContextProvider = viewModelContextProvider;
    }

    private static IEnumerable<(string Name, Draft Audio)> SelectAudios(IEnumerable<Passage> passages, ParentAudioType audioType)
    {
        var audios = audioType switch
        {
            ParentAudioType.Draft => passages.Select(passage => (
                passage.PassageNumber.PassageNumberString,
                passage.CurrentDraftAudio)),
            ParentAudioType.PassageBackTranslation => passages.Select(passage => (
                passage.PassageNumber.PassageNumberString,
                passage.CurrentDraftAudio.RetellBackTranslationAudio as Draft)),
            ParentAudioType.SegmentBackTranslation => passages
                .SelectMany(passage => passage.CurrentDraftAudio.SegmentBackTranslationAudios
                    .OrderBy(sbt => sbt.TimeMarkers.StartMarkerTime))
                .Select((sbt, index) => ($"{index + 1}", sbt as Draft)),
            ParentAudioType.PassageBackTranslation2 => passages.Select(passage => (
                passage.PassageNumber.PassageNumberString,
                passage.CurrentDraftAudio.RetellBackTranslationAudio?.RetellBackTranslationAudio as Draft)),
            ParentAudioType.SegmentBackTranslation2 => passages
                .SelectMany(passage => passage.CurrentDraftAudio.SegmentBackTranslationAudios
                    .OrderBy(sbt => sbt.TimeMarkers.StartMarkerTime))
                .Select((sbt, index) => ($"{index + 1}", sbt.RetellBackTranslationAudio as Draft)),
            _ => throw new ArgumentOutOfRangeException(nameof(audioType), audioType, null)
        };

        return audios;
    }

    public static IEnumerable<Draft> SelectDraftAudios(IEnumerable<Passage> passages, ParentAudioType audioType)
    {
        return SelectAudios(passages, audioType).Select(a => a.Audio).Where(a => a is not null);
    }

    public PlayerAudioModel[] SelectAudioModels(IEnumerable<Passage> passages, ParentAudioType audioType, Predicate<Conversation> conversationsFilter, bool requireNoteListen)
    {
        var audios = SelectAudios(passages, audioType);

        return audios.Where(a => a.Audio != null)
            .Select(a => a.Audio.CreatePlayerAudioModel(
                audioType: audioType,
                name: a.Name,
                path: _viewModelContextProvider.GetTempAudioService(a.Audio).SaveTempAudio(),
                conversationsFilter: conversationsFilter,
                userId: _viewModelContextProvider.GetLoggedInUser().Id,
                requireNoteListen: requireNoteListen))
            .ToArray();
    }
}