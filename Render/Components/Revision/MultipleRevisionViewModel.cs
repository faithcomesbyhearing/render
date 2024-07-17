using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Models.Snapshot;
using Render.Repositories.SnapshotRepository;
using Render.Resources.Localization;
using System.Collections.ObjectModel;


namespace Render.Components.Revision
{
    public class MultipleRevisionViewModel : ActionViewModelBase
    {
        private const string MultipleRevisionUrlPathSegment = "CurrentRevisionState";
        private readonly ISnapshotRepository _snapshotRepository;

        [Reactive] public bool IsCurrentRevision { get; private set; }
        [Reactive] public KeyValuePair<Snapshot, string> SelectedRevisionItem { get; private set; }
        public ObservableCollection<KeyValuePair<Snapshot, string>> RevisionItems { get; } = new();
        public Snapshot SelectedSnapshot => SelectedRevisionItem.Key;

        public MultipleRevisionViewModel(
            ActionState actionState,
            IViewModelContextProvider viewModelContextProvider) :
            base(actionState, MultipleRevisionUrlPathSegment, viewModelContextProvider)
        {
            _snapshotRepository = ViewModelContextProvider.GetSnapshotRepository();

            Disposables.Add(this
                .WhenAnyValue(x => x.IsCurrentRevision)
                .Subscribe(isCurrent =>
                {
                    ActionState = isCurrent ? ActionState.Optional : ActionState.Required;
                }));
        }

        public async Task SelectSnapshot(KeyValuePair<Snapshot, string> selectedRevision, bool getRetellBackTranslations = false, 
            bool getSegmentBackTranslations = false)
        {
            SelectedRevisionItem = selectedRevision;
            var selectedSnapshot = SelectedRevisionItem.Key;
            selectedSnapshot = await _snapshotRepository.GetPassageDraftsForSnapshot(selectedSnapshot, getRetellBackTranslations, getSegmentBackTranslations);

            IsCurrentRevision = selectedSnapshot.Equals(RevisionItems.FirstOrDefault().Key);
        }

        public async Task<List<Snapshot>> FillRevisionItems(Guid sectionId, Guid stageId)
        {
            var snapshots = await _snapshotRepository.GetSnapshotsForSectionAsync(sectionId);

            var filteredSnapshots = _snapshotRepository.FilterSnapshotByStageId(snapshots, stageId);

            for (var index = 0; index < filteredSnapshots.Count; index++)
            {
                if (index == filteredSnapshots.Count - 1)
                {
                    RevisionItems.Insert(0, new KeyValuePair<Snapshot, string>(filteredSnapshots[index],
                        string.Format(AppResources.Current, string.Empty).Trim()));
                }
                else
                {
                    RevisionItems.Add(new KeyValuePair<Snapshot, string>(filteredSnapshots[index],
                        string.Format(AppResources.DraftTitle, index + 1)));
                }
            }

            SelectedRevisionItem = RevisionItems.First();

            return snapshots;
        }

        public override void Dispose()
        {
            RevisionItems.Clear();
            SelectedRevisionItem = default;

            base.Dispose();
        }

    }
}
