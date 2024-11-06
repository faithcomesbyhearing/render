using DynamicData;
using Render.Components.BarPlayer;
using Render.Components.MiniWaveformPlayer;
using Render.Kernel;
using System.Collections.ObjectModel;

namespace Render.Components.SectionInfo
{
    public class SectionInfoPlayerViewModel : ViewModelBase
    {
        public IMiniWaveformPlayerViewModel DraftAudioPlayer { get; set; }
        public ObservableCollection<IBarPlayerViewModel> RetellAudioPlayers { get; private set; } = new();

        public SectionInfoPlayerViewModel(
            IViewModelContextProvider viewModelContextProvider,
            IMiniWaveformPlayerViewModel draftPlayer,
            IEnumerable<IBarPlayerViewModel> retellPlayers)
            : base(
                  "SectionInfoPlayerViewModel",
                  viewModelContextProvider)
        {
            DraftAudioPlayer = draftPlayer;
            RetellAudioPlayers.AddRange(retellPlayers);
        }

        public override void Dispose()
        {
            DraftAudioPlayer?.Dispose();
            DraftAudioPlayer = null;

            foreach (var player in RetellAudioPlayers)
            {
                player.Dispose();
            }
            RetellAudioPlayers.Clear();

            base.Dispose();
        }
    }
}
