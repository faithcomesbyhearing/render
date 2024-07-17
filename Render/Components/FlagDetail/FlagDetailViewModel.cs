using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Components.BarPlayer;
using Render.Components.Navigation;
using Render.Kernel;
using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;
using Render.Resources;
using Render.Services;

namespace Render.Components.FlagDetail;

public class FlagDetailViewModel : PopupViewModelBase<bool>
{
    private readonly bool _required;
    private CommunityTestForStage _communityTestForStage;
    public ObservableCollection<QuestionAndResponseViewModel> QuestionAndResponses { get; private set; }
    public ObservableCollection<RetellViewModel> Retells { get; private set; }
    public ObservableCollection<Response> Responses { get; private set; }
	public ItemDetailNavigationViewModel FlagDetailNavigationViewModel { get; private set; }

    [Reactive] public Flag Flag { get; private set; }
    public Passage Passage { get; private set; }
    public string Glyph { get; set; }

    public ReactiveCommand<Unit, Unit> CloseModalCommand { get; }
    public ReactiveCommand<Flag, Unit> InitCommand { get; }
    public ReactiveCommand<Unit, Unit> OnCloseCommand { get; set; }

    public int SectionNumber { get; }

    public static async Task<FlagDetailViewModel> CreateAsync(Flag flag,
        Passage passage,
        CommunityTestForStage communityTestForStage,
        int sectionNumber,
        bool required,
        IViewModelContextProvider viewModelContextProvider,
        ReactiveCommand<Unit, Unit> onCloseCommand = null)
    {
        var vm = new FlagDetailViewModel(flag, passage, communityTestForStage, sectionNumber, required, viewModelContextProvider, onCloseCommand);
        await vm.InitCommand.Execute(flag);
        return vm;
    }

    private FlagDetailViewModel(Flag flag,
        Passage passage,
        CommunityTestForStage communityTestForStage,
        int sectionNumber,
        bool required,
        IViewModelContextProvider viewModelContextProvider,
        ReactiveCommand<Unit, Unit> onCloseCommand = null) : base(string.Empty, viewModelContextProvider)
    {
        _required = required;

        Glyph = ResourceExtensions.GetResourceValue<string>(Icon.Flag.ToString());
        SectionNumber = sectionNumber;
        InitCommand = ReactiveCommand.CreateFromTask<Flag>(InitAsync);
        CloseModalCommand = ReactiveCommand.CreateFromTask(CloseConversationAsync);
        OnCloseCommand = onCloseCommand;

        _communityTestForStage = communityTestForStage;
        Flag = flag;
        Passage = passage;

        Disposables.Add(this.WhenAnyValue(vm => vm.Flag)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .WhereNotNull()
            .InvokeCommand(InitCommand));
    }

    private async Task InitAsync(Flag flag)
    {
        var questions = new List<QuestionAndResponseViewModel>();
        await Task.Run(() =>
        {
            for (int i = 0; i < flag.Questions.Count; i++)
            {
                questions.Add(new QuestionAndResponseViewModel(flag.Questions[i], i + 1, _required, ViewModelContextProvider));
            }
        });

        if (_communityTestForStage.Responses.Any())
        {
			await Task.Run(() =>
			{
				var question = new Question(_communityTestForStage.StageId);
				question.AddResponses(_communityTestForStage.Responses.ToList());
				questions.Add(new QuestionAndResponseViewModel(question, _required, ViewModelContextProvider));
			});
		}
		QuestionAndResponses = QuestionAndResponses ?? new ObservableCollection<QuestionAndResponseViewModel>();
        QuestionAndResponses.Clear();
        QuestionAndResponses.AddRange(questions);

        var retells = new List<RetellViewModel>();

        await Task.Run(() =>
        {
            for (int index = 0; index < _communityTestForStage.Retells.Count; index++)
            {
                var retell = _communityTestForStage.Retells[index];
                retells.Add(new RetellViewModel(retell, index + 1, _required, ViewModelContextProvider));
            }
        });

        Retells = Retells ?? new ObservableCollection<RetellViewModel>();
        Retells.Clear();
        Retells.AddRange(retells);

		Disposables.AddRange(Retells);
        Disposables.AddRange(QuestionAndResponses);
    }

    private async Task CloseConversationAsync()
    {
        PauseAllAudios();
        
        if (OnCloseCommand != null)
        {
            await OnCloseCommand.Execute();
        }
    
        ClosePopup(Flag.Questions.Any());
    }
    
    private void PauseAllAudios(IBarPlayerViewModel exceptPlayer = null)
    {
        foreach (var retell in Retells)
        {
            retell.PauseAllAudios(exceptPlayer);
        }

        foreach (var question in QuestionAndResponses)
        {
            question.PauseAllAudios(exceptPlayer);
        }
    }

    public void SetNavigation(ItemDetailNavigationViewModel navigationViewModel)
    {
        FlagDetailNavigationViewModel = navigationViewModel;
        FlagDetailNavigationViewModel.OnBeforeChangeItemCommand = ReactiveCommand.Create(() => PauseAllAudios());
        
        Disposables.Add(this.WhenAnyValue(x => x.FlagDetailNavigationViewModel.CurrentItem)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(flag => { Flag = flag as Flag; }));

        Disposables.Add(FlagDetailNavigationViewModel.MoveToNextItemCommand
            .IsExecuting
            .CombineLatest(InitCommand.IsExecuting)
            .Select((isExecutings) => isExecutings.First || isExecutings.Second)
            .Subscribe(isExecuting => IsLoading = isExecuting));

        Disposables.Add(FlagDetailNavigationViewModel.MoveToPreviousItemCommand
            .IsExecuting
            .CombineLatest(InitCommand.IsExecuting)
            .Select((isExecutings) => isExecutings.First || isExecutings.Second)
            .Subscribe(isExecuting => IsLoading = isExecuting));
    }

    public override void Dispose()
    {
        Flag = null;
        Passage = null;
        Retells = null;
        QuestionAndResponses = null;
        _communityTestForStage = null;

        base.Dispose();
    }
}