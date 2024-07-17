using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Models.Sections;
using Render.Models.Snapshot;
using Render.Resources.Localization;

namespace Render.Pages.Settings.AudioExport
{
    public class SectionToExport : ReactiveObject, IDisposable
    {
        [Reactive] public bool Selected { get; private set; }
        public Section Section { get; private set; }
		public Snapshot Snapshot { get; private set; }
		public bool HasSnapshot { get; }
        public bool IsEmpty { get;  }
        [Reactive] public bool ShowSeparator { get; set; } = true;
        public string NoAudioText { get; }

        public readonly ReactiveCommand<Unit, Unit> SelectCommand;

        public SectionToExport(Section section, Snapshot snapshot, bool isEmpty = false)
        {
            if (snapshot != null && snapshot.Temporary)
            {
                throw new ArgumentException("Temporary snapshot is not allowed for audio export.");
            }
            
            Section = section;
            Snapshot = snapshot;
            HasSnapshot = Snapshot != null;
            IsEmpty = isEmpty;
            ShowSeparator = !IsEmpty;
            NoAudioText = HasSnapshot == false ? AppResources.NoAudio : "";
            SelectCommand = ReactiveCommand.Create(() => { Selected = !Selected; }, 
                    this.WhenAnyValue(x => x.HasSnapshot));
        }

        public void Select(bool select)
        {
            if (HasSnapshot)
            {
                Selected = select;
            }
        }

        public void Dispose()
		{
			Snapshot = null;
			Section = null;
			SelectCommand?.Dispose();
		}
    }
}