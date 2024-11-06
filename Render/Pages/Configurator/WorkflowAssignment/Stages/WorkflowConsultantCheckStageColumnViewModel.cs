using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Render.Kernel;
using Render.Kernel.WrappersAndExtensions;
using Render.Models.Users;
using Render.Models.Workflow;
using Render.Models.Workflow.Stage;
using Render.Repositories.Extensions;

namespace Render.Pages.Configurator.WorkflowAssignment.Stages;

public class WorkflowConsultantCheckStageColumnViewModel : WorkflowStageColumnViewModel
{
    private readonly List<Team> _teams;

    [Reactive] public TeamAssignmentCardViewModel BackTranslateAssignmentCard { get; private set; }
    [Reactive] public TeamAssignmentCardViewModel BackTranslate2AssignmentCard { get; private set; }
    [Reactive] public TeamAssignmentCardViewModel NoteTranslateAssignmentCard { get; private set; }
    [Reactive] public TeamAssignmentCardViewModel TranscribeAssignmentCard { get; private set; }
    [Reactive] public TeamAssignmentCardViewModel Transcribe2AssignmentCard { get; private set; }
    [Reactive] public TeamAssignmentCardViewModel ConsultantAssignmentCard { get; private set; }

    [Reactive] private bool RetellIsActive { get; set; }
    [Reactive] private bool DoPassageTranscribeIsActive { get; set; }
    [Reactive] private bool SegmentIsActive { get; set; }
    [Reactive] private bool SegmentTranscribeIsActive { get; set; }

    [Reactive] public bool ShowBackTranslateCard { get; set; }

    [Reactive] private bool Retell2IsActive { get; set; }
    [Reactive] private bool DoPassageTranscribe2IsActive { get; set; }
    [Reactive] private bool Segment2IsActive { get; set; }
    [Reactive] private bool SegmentTranscribe2IsActive { get; set; }
    [Reactive] public bool ShowBackTranslate2Card { get; set; }
    [Reactive] public bool ShowNoteTranslateCard { get; set; }

    [Reactive] public bool ShowTranscribeCard { get; set; }
    [Reactive] public bool ShowTranscribe2Card { get; set; }

    public string ConsultantCheckStepName { get; }
    public bool HasAnyCustomInterpretStepName { get; }
    public string InterpretToTranslatorStepName { get; }
    public string InterpretToConsultantStepName { get; }
    public string BackTranslateStepName { get; }
    public string BackTranslate2StepName { get; }
    public string TranscribeStepName { get; }
    public string Transcribe2StepName { get; }

    public WorkflowConsultantCheckStageColumnViewModel(
        Stage stage,
        IReadOnlyList<Team> teamList,
        Action<Team> onTeamDeleted,
        Action<Guid, Team> onTranslationTeamUpdate,
        Action<Stage> updateStageColumnCallback,
        IList<IUser> users,
        RenderWorkflow workflow,
        IViewModelContextProvider viewModelContextProvider,
        string projectName)
        : base(
            stage: stage,
            workflow: workflow,
            updateStageColumnCallback: updateStageColumnCallback,
            viewModelContextProvider: viewModelContextProvider,
            projectName: projectName)
    {
        var stageId = stage.Id;
        _teams = new List<Team>(teamList);
        var team = _teams.First();
        var interpretToTranslatorStep = Steps.SingleOrDefault(x => x.RenderStepType == RenderStepTypes.InterpretToTranslator);
        var interpretToConsultantStep = Steps.SingleOrDefault(x => x.RenderStepType == RenderStepTypes.InterpretToConsultant);
        var btStep = Steps.FirstOrDefault(x => x.RenderStepType == RenderStepTypes.BackTranslate);
        var transcribeStep = Steps.FirstOrDefault(x => x.RenderStepType == RenderStepTypes.Transcribe);

        HasAnyCustomInterpretStepName = interpretToTranslatorStep?.CustomName?.IsNullOrEmpty() is false
                                        || interpretToConsultantStep?.CustomName?.IsNullOrEmpty() is false;

        InterpretToTranslatorStepName = interpretToTranslatorStep?.GetName();
        InterpretToConsultantStepName = interpretToConsultantStep?.GetName();
        BackTranslateStepName = BackTranslate2StepName = btStep?.GetName();
        TranscribeStepName = Transcribe2StepName = transcribeStep?.GetName();
        ConsultantCheckStepName = Steps.SingleOrDefault(x => x.RenderStepType == RenderStepTypes.ConsultantCheck)?.GetName();

        //Get all steps for the stage
        var steps = stage.GetAllWorkflowEntrySteps(false);
        var multiStep = stage.Steps.First(x => x.Order == Step.Ordering.Parallel).GetSubSteps()
            .First(x => x.Role == Roles.BackTranslate);

        #region Setup for 1st segment back translate role card

        var firstBt = steps
            .FirstOrDefault(x => x.Role == Roles.BackTranslate) ?? multiStep.GetSubSteps()
            .First(x => x.RenderStepType == RenderStepTypes.BackTranslate);
        var segmentActiveSetting =
            firstBt.StepSettings.Settings.First(x => x.SettingType == SettingType.DoSegmentBackTranslate);
        var retellActiveSetting =
            firstBt.StepSettings.Settings.First(x => x.SettingType == SettingType.DoRetellBackTranslate);
        ShowBackTranslateCard = firstBt.IsActive();

        Disposables.Add(segmentActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => SegmentIsActive = b));
        Disposables.Add(retellActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => RetellIsActive = b));
        Disposables.Add(this.WhenAnyValue(x => x.SegmentIsActive,
                x => x.RetellIsActive)
            .Subscribe(args => ShowBackTranslateCard = args.Item1 || args.Item2));

        IUser btUser = null;
        var btAssignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.BackTranslate);
        if (btAssignment != null)
        {
            btUser = users.FirstOrDefault(x => x.Id == btAssignment.UserId);
        }

        BackTranslateAssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.BackTranslate, _teams,
            onTeamDeleted, onTranslationTeamUpdate, workflow,
            viewModelContextProvider, btUser);

        #endregion

        #region Setup for 2nd segment back translate role card

        var secondBt = steps.FirstOrDefault(x => x.Role == Roles.BackTranslate2) ??
                       multiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                           .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                           .First(x => x.Role == Roles.BackTranslate2);
        var secondSegmentSetting =
            secondBt.StepSettings.Settings.First(x => x.SettingType == SettingType.DoSegmentBackTranslate);
        var secondRetellSetting =
            secondBt.StepSettings.Settings.First(x => x.SettingType == SettingType.DoRetellBackTranslate);
        ShowBackTranslate2Card = secondBt.IsActive();
        Disposables.Add(secondSegmentSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => Segment2IsActive = b));
        Disposables.Add(secondRetellSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => Retell2IsActive = b));
        Disposables.Add(this.WhenAnyValue(x => x.Segment2IsActive,
                x => x.Retell2IsActive)
            .Subscribe(args => ShowBackTranslate2Card = args.Item1 || args.Item2));

        IUser bt2User = null;
        var bt2Assignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.BackTranslate2);
        if (bt2Assignment != null)
        {
            bt2User = users.FirstOrDefault(x => x.Id == bt2Assignment.UserId);
        }

        BackTranslate2AssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.BackTranslate2, _teams, onTeamDeleted, onTranslationTeamUpdate, workflow,
            viewModelContextProvider, bt2User);

        #endregion

        #region Setup for note translate role card

        var ntStep = stage.Steps.First(x => x.Role == Roles.NoteTranslate);
        ShowNoteTranslateCard = ntStep.IsActive();
        var ntStepSetting = ntStep.StepSettings.Settings.First(x => x.SettingType == SettingType.IsActive);
        Disposables.Add(ntStepSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => ShowNoteTranslateCard = b));

        IUser ntUser = null;
        var ntAssignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.NoteTranslate);
        if (ntAssignment != null)
        {
            ntUser = users.FirstOrDefault(x => x.Id == ntAssignment.UserId);
        }

        NoteTranslateAssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.NoteTranslate, _teams,
            onTeamDeleted, onTranslationTeamUpdate, workflow,
            viewModelContextProvider, ntUser);

        #endregion

        #region Setup for 1st segment of transcribe roles card

        var firstTranscribeSegment = steps.First(x => x.Role == Roles.Transcribe);
        var transcribeSegmentActiveSetting =
            firstTranscribeSegment.StepSettings.Settings.First(x => x.SettingType == SettingType.DoSegmentTranscribe);
        var transcribePassageActiveSetting = steps.Last(x => x.Role == Roles.Transcribe).StepSettings.Settings.First(x => x.SettingType == SettingType.DoPassageTranscribe);
        ShowTranscribeCard = firstTranscribeSegment.IsActive();

        Disposables.Add(transcribeSegmentActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => SegmentTranscribeIsActive = b));
        Disposables.Add(transcribePassageActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => DoPassageTranscribeIsActive = b));
        Disposables.Add(this.WhenAnyValue(x => x.SegmentTranscribeIsActive,
                x => x.DoPassageTranscribeIsActive)
            .Subscribe(args => ShowTranscribeCard = args.Item1 || args.Item2));

        IUser tUser = null;
        var tAssignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Transcribe);
        if (tAssignment != null)
        {
            tUser = users.FirstOrDefault(x => x.Id == tAssignment.UserId);
        }

        TranscribeAssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.Transcribe, _teams,
            onTeamDeleted, onTranslationTeamUpdate, workflow,
            viewModelContextProvider, tUser);

        #endregion

        #region Setup for 2nd segment of transcribe roles card

        var secondTranscribeSegment = steps.FirstOrDefault(x => x.Role == Roles.Transcribe2);
        Step secondTranscribeRetell;
        if (secondTranscribeSegment != null)
        {
            secondTranscribeRetell = steps.Last(x => x.Role == Roles.Transcribe2);
        }
        else
        {
            secondTranscribeSegment = multiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.Transcribe2);
            secondTranscribeRetell = multiStep.GetSubSteps().First(x => x.Order == Step.Ordering.Parallel)
                .GetSubSteps().First(x => x.GetSubSteps().Count > 0).GetSubSteps()
                .First(x => x.Role == Roles.Transcribe2);
        }

        var secondTranscribeSegmentActiveSetting =
            secondTranscribeSegment.StepSettings.Settings.First(x => x.SettingType == SettingType.IsActive);
        var secondTranscribeRetellActiveSetting = secondTranscribeRetell.StepSettings.Settings.First(x => x.SettingType == SettingType.IsActive);
        ShowTranscribe2Card = secondTranscribeSegment.IsActive();

        Disposables.Add(secondTranscribeSegmentActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => SegmentTranscribe2IsActive = b));
        Disposables.Add(secondTranscribeRetellActiveSetting.WhenAnyValue(x => x.Value)
            .Subscribe(b => DoPassageTranscribe2IsActive = b));
        Disposables.Add(this.WhenAnyValue(x => x.SegmentTranscribe2IsActive,
                x => x.DoPassageTranscribe2IsActive)
            .Subscribe(args => ShowTranscribe2Card = args.Item1 || args.Item2));

        IUser t2User = null;
        var t2Assignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Transcribe2);
        if (t2Assignment != null)
        {
            t2User = users.FirstOrDefault(x => x.Id == t2Assignment.UserId);
        }

        Transcribe2AssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.Transcribe2, _teams,
            onTeamDeleted, onTranslationTeamUpdate, workflow,
            viewModelContextProvider, t2User);

        #endregion

        #region Setup for consultant role card

        IUser consultantUser = null;
        var consultantAssignment = team.GetWorkflowAssignmentForStageAndRole(stage.Id, Roles.Consultant);
        if (consultantAssignment != null)
        {
            consultantUser = users.FirstOrDefault(x => x.Id == consultantAssignment.UserId);
        }

        ConsultantAssignmentCard = new TabletTeamAssignmentCardViewModel(stageId, Roles.Consultant, _teams,
            onTeamDeleted,
            onTranslationTeamUpdate,
            workflow,
            viewModelContextProvider, consultantUser);

        #endregion

        Disposables.Add(this.WhenAnyValue(
            x => x.BackTranslateAssignmentCard.UserCardViewModel,
            x => x.BackTranslate2AssignmentCard.UserCardViewModel,
            x => x.NoteTranslateAssignmentCard.UserCardViewModel,
            x => x.TranscribeAssignmentCard.UserCardViewModel,
            x => x.Transcribe2AssignmentCard.UserCardViewModel,
            x => x.ConsultantAssignmentCard.UserCardViewModel).Subscribe(x =>
        {
            AllWorkAssigned =
                (!ShowBackTranslateCard || x.Item1 != null) &&
                (!ShowBackTranslate2Card || x.Item2 != null) &&
                (!ShowNoteTranslateCard || x.Item3 != null) &&
                (!ShowTranscribeCard || x.Item4 != null) &&
                (!ShowTranscribe2Card || x.Item5 != null) && x.Item6 != null;
        }));
    }

    public override void AddTeamToList(IList<IUser> users, Team team, IUser user,
        Action<Team> onTeamDeleted,
        Action<Guid, Team> onTranslationTeamUpdate)
    {
        _teams.Add(team);
    }

    public override void RemoveTeamFromList(Team team)
    {
        _teams.Remove(team);
    }
}