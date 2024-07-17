using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Sections;

namespace Render.Pages.Settings.SectionStatus.Processes
{
    public class SectionCardViewModel : ViewModelBase
    {

        private  Func<Guid, Task> SectionSelectCallback;

        public ReactiveCommand<Unit, Unit> SelectSectionCommand { get; private set; }

        [Reactive] public Section Section { get; private set; }
        [Reactive] public string ScriptureRange { get; set; }
        [Reactive] public bool IsSelected { get; set; }
        [Reactive] public bool IsApproved { get; set; }
        [Reactive] public bool HasConflict { get; set; }
        [Reactive] public bool IsLastSectionCard { get; set; }

        public SectionCardViewModel(
            Section section,
            IViewModelContextProvider viewModelContextProvider,
            Func<Guid, Task> sectionSelectCallback) : base("SectionStatusSectionCard", viewModelContextProvider)
        {
            try
            {
                Section = section;
                HasConflict = false;
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

                SectionSelectCallback = sectionSelectCallback;
                SelectSectionCommand = ReactiveCommand.Create(SelectSection);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }
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
                SectionSelectCallback.Invoke(Section.Id);
            }
        }

        public override void Dispose()
        {
            Section = null;

            SelectSectionCommand?.Dispose();
            SelectSectionCommand = null;

            SectionSelectCallback = null;

            base.Dispose();
        }
    }
}