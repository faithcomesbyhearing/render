using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Workflow.Stage;
using System.Reactive;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class SectionCardViewModel : ViewModelBase
    {
        private Func<SectionCardViewModel, Task> _sectionSelectCallback;
        private Action<SectionCardViewModel> _sectionSelectToExportCallback;

        [Reactive]
        public Section Section { get; private set; }

        [Reactive]
        public string ScriptureRange { get; set; }

        [Reactive]
        public bool IsSelected { get; set; }

        [Reactive]
        public bool IsApproved { get; set; }

        [Reactive]
        public bool HasConflict { get; set; }

        [Reactive]
        public bool IsLastSectionCard { get; set; }

        [Reactive]
        public bool SelectedToExport { get; set; }

        public Guid ViewModelId { get; private set; } = Guid.NewGuid();

        public ReactiveCommand<Unit, Unit> SelectSectionToExportCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectSectionCommand { get; private set; }

        public bool IsEmpty { get; }

        public Stage StageFrom { get; set; }

        public SectionCardViewModel(
            Section section,
            IViewModelContextProvider viewModelContextProvider,
            Func<SectionCardViewModel, Task> sectionSelectCallback,
            Action<SectionCardViewModel> selectSectionToExportCallback = null,
            Stage stageFrom = null)
            : base("SectionStatusSectionCard", viewModelContextProvider)
        {
            Section = section;
            HasConflict = false;
            IsEmpty = section is null
                || section.Passages.Any(p => p.CurrentDraftAudioId != Guid.Empty) is false;     // using CurrentDraftAudioId instead of CurrentDraftAudio
                                                                                                // because setting of CurrentDraftAudio property happens
                                                                                                // at the moment of SectionRepository.GetSectionWithDraftsAsync()

            StageFrom = stageFrom;

            Disposables
                .Add(this.WhenAnyValue(x => x.Section)
                    .WhereNotNull()
                    .Subscribe(s =>
                    {
                        if (s.ApprovedBy != Guid.Empty)
                        {
                            IsApproved = true;
                        }

                        ScriptureRange = s.ScriptureReference;
                    }));

            _sectionSelectCallback = sectionSelectCallback;
            _sectionSelectToExportCallback = selectSectionToExportCallback;
            SelectSectionCommand = ReactiveCommand.Create(SelectSection);
            SelectSectionToExportCommand = ReactiveCommand.Create(SelectSectionToExport);
        }

        public void SetSection(Section section)
        {
            Section = section;
            IsApproved = section.ApprovedBy != Guid.Empty;
        }

        private void SelectSection()
        {
            if (Section != null)
            {
                _sectionSelectCallback.Invoke(this);
            }
        }

        private void SelectSectionToExport()
        {
            if (Section != null)
            {
                _sectionSelectToExportCallback.Invoke(this);
            }
        }

        public override void Dispose()
        {
            Section = null;

            SelectSectionToExportCommand?.Dispose();
            SelectSectionToExportCommand = null;
            SelectSectionCommand?.Dispose();
            SelectSectionCommand = null;

            _sectionSelectCallback = null;
            _sectionSelectToExportCallback = null;

            base.Dispose();
        }
    }
}