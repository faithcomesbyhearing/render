using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;

namespace Render.Pages.Translator.DraftingPage
{
    public class DraftReplacementComponentViewModel : ViewModelBase
    {

        public DynamicDataWrapper<DraftReplacementCardViewModel> DraftCards;
        public DraftViewModel SelectedDraft { get; private set; }

        public ReactiveCommand<Unit, bool> ReplaceAudioCommand { get; }

        [Reactive] 
        public bool EnableReplace { get; private set; }

        public DraftReplacementComponentViewModel(
            ReadOnlyObservableCollection<DraftViewModel> drafts,
            IViewModelContextProvider viewModelContextProvider) 
            : base("DraftModal", viewModelContextProvider)
        {
            DraftCards = new DynamicDataWrapper<DraftReplacementCardViewModel>();
            foreach (var draft in drafts)
            {
                DraftCards.Add(new DraftReplacementCardViewModel(draft, ViewModelContextProvider));
            }

            ReplaceAudioCommand = ReactiveCommand.Create(ReplaceAudio, this.WhenAnyValue(vm => vm.EnableReplace));

            Disposables.Add(DraftCards.Observable
                .WhenPropertyChanged(x => x.Selected)
                .Select(c => c.Sender)
                .Subscribe(vm =>
                {
                    if (vm.Selected is false)
                    {
                        return;
                    }

                    EnableReplace = true;

                    foreach (var draft in DraftCards.SourceItems)
                    {
                        if (draft.Title != vm.Title)
                        {
                            draft.Deselect();
                        }
                    }
                }));
        }
        
        private bool ReplaceAudio()
        {
            if (EnableReplace)
            {
                var selectedCard = DraftCards.SourceItems.SingleOrDefault(x => x.Selected);

                if (selectedCard != null)
                {
                    SelectedDraft = selectedCard.DraftViewModel;
                    return true;
                }

                return false;
            }

            return false;
        }

        public override void Dispose()
        {
            DraftCards?.Dispose();
            DraftCards = null;

            base.Dispose();
        }
    }
}