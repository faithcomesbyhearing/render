using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;

namespace Render.Pages.Translator.DraftingPage
{
    public class DraftReplacementCardViewModel : ViewModelBase
    {
        public DraftViewModel DraftViewModel { get; private set; }

        public string Title { get; }

        public bool IsPreviousDraft;

        [Reactive]
        public bool Selected { get; private set; }

        public ReactiveCommand<Unit, Unit> SelectCommand { get; private set; }

        public DraftReplacementCardViewModel(DraftViewModel draftViewModel, IViewModelContextProvider viewModelContextProvider)
            : base("DraftReplacementCard", viewModelContextProvider)
        {
            DraftViewModel = draftViewModel;
            Title = draftViewModel.Number.ToString();
            Selected = false;
            IsPreviousDraft = draftViewModel.IsPreviousDraft;
            SelectCommand = ReactiveCommand.Create(Select);
        }

        public void Select()
        {
            if (!IsPreviousDraft)
            {
                Selected = true;
            }
        }

        public void Deselect()
        {
            if (!IsPreviousDraft)
            {
                Selected = false;
            }
        }

        public override void Dispose()
        {
            DraftViewModel = null;
            base.Dispose();
        }
    }
}