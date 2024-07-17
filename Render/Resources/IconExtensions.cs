﻿using Render.Resources.Styles;

namespace Render.Resources
{
    [ContentProperty(nameof(Icon))]
    public class IconExtensions : IMarkupExtension<string>
    {
        public Icon Icon { get; set; }

        public string ProvideValue(IServiceProvider serviceProvider)
        {
            var iconGlyph = ResourceExtensions.GetResourceValue(Icon.ToString());

            return (string)iconGlyph;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }

        public static string GetIconGlyph(Icon icon)
        {
            return (string)ResourceExtensions.GetResourceValue(icon.ToString());
        }

        public static FontImageSource BuildFontImageSource(Icon icon, Color color = default, double size = 25)
        {
            if (color == default)
            {
                color = (ColorReference)ResourceExtensions.GetResourceValue("MainIconColor") == null ?
                    new Color() : 
                    ((ColorReference)ResourceExtensions.GetResourceValue("MainIconColor")).Color;
            }
            var iconGlyph = ResourceExtensions.GetResourceValue(icon.ToString());
            var fontImageSource = new FontImageSource
            {
                FontFamily = "Icons",
                Glyph = (string)iconGlyph,
                Color = color,
                Size = size,
                AutomationId = $"{iconGlyph}ImageSource"
            };

            return fontImageSource;
        }
    }

    public enum Icon
    {
        Placeholder,
        Sync,
        AddFlag,
        AddOrAppend,
        AddUser,
        Add,
        AdvisorCheck,
        ArrowLeftCircle,
        ArrowRightCircle,
        AssignAllSections,
        SegmentBackTranslate,
        CancelOrClose,
        Checkmark,
        CirclePlay,
        CircleStop,
        Combine,
        CommunityCheck1,
        CommunityCheck2,
        CommunityCheck3,
        CommunityCheckFlag,
        CommunityCheckQAndR,
        CommunityCheckRetell,
        CommunityCheckSetup,
        CommunityCheck,
        CommunityRevise,
        ConsultantCheckOriginal,
        ConsultantCheck,
        ConsultantRevise,
        CutConfirm,
        CutMark,
        CutUnmark,
        DecreaseFontSize,
        DeleteAudio,
        DeleteUser,
        Delete,
        DivisionOrCut,
        EditFlag,
        EditUser,
        Edit,
        Exit,
        ExportNote,
        ExportProject,
        ExportSection,
        Export,
        Filter,
        FinishedPassOrSubmit,
        Flag,
        Home,
        IncreaseFontSize,
        LeftArrow,
        Link,
        Listen,
        LoadSectionsFaster,
        Lock,
        Logo,
        LogOut,
        LoopPhone,
        Loop,
        MorePreferences,
        MoreRoles,
        MenuVertical,
        NoteTranslate,
        Note,
        NoticeToUser,
        ObserveReadOnly,
        Open,
        PassageReview,
        Passage,
        PeerReview,
        PeerRevise,
        PleaseTryAgain,
        PrimaryGeneral,
        PrimaryResource,
        ProcessesSetup,
        Processes,
        ProjectConfiguration,
        ProjectInformation,
        ProjectTechnician,
        Project,
        RecordANoteOrSuggestRevision,
        Record,
        RecorderAppend,
        RecorderPause,
        RecorderPlay,
        RecorderRecord,
        RecorderStop,
        RemoveFlag,
        Remove,
        ReRecord,
        Restart,
        RetellBackTranslate,
        Review,
        RightArrow,
        Roles,
        Sections,
        SelectOrAssign,
        Separate,
        SetReview,
        SubmitFlag,
        SubtractOrMinus,
        TallyI,
        TallyII,
        TallyIII,
        Toolbox,
        Transcribe,
        Translate,
        Type,
        UnassignAllSections,
        Undo,
        UnselectAll,
        Update,
        User,
        ViewUser,
        View,
        TypeWarning,
        Settings,
        ChevronLeft,
        ChevronRight,
        SelectATest,
        Sequence,
        StarEdit,
        StarFilled,
        Star,
        TeamSingular,
        TeamsIcon,
        WorkflowAddStep,
        WorkflowArrangeSteps,
        ConsultantApproval,
        DraftListen,
        DraftSelect,
        Draft,
        EditNotes,
        Generate,
        GenericCheck,
        LoadSetsFaster,
        QrReview,
        ManageDivisions,
        PassageDraftReview,
        PassageListen,
        PassageTranslate,
        Reference,
        SectionAssignUser,
        SectionFeedback,
        SectionReview,
        LoadingSpinner,
        ReorderTwo,
        ProjectList,
        AddProject,
        DownloadItem,
        OffloadItem,
        CopyStageQuestions,
        CopyQuestionsTo,
        CopyQuestionsFrom,
        ArrowCopyQuestions,
        ExportAudio,
        DraftEmpty,
        BackTranslate,
        SectionStatus,
        SnapshotRecovery,
        RestoreSnapshot,
        SelectedQuestionSet,
        MicrophoneNotFound,
        Plus,
        Minus,
        RemoveCut,
        ManualAudioEdit,
        CancelCut,
        AddCut,
        ClearBothSnapshots,
        SelectASnapshot,
        ReassignTeam,
        UserSettings,
        DeleteUndo,
        MicrophoneWarning,
        SectionsUnreviewed,
        InterpretToConsultant,
        InterpretToTranslator,
        EmptyProject,
        NoOffloadProject,
        DeleteAndComplete,
        DeleteImmediately,
        DeleteWarning,
        SectionNew,
        PassageNew,
        DraftsNew,
        Union,
        InternetError,
        AddProjectViaId,
        InvalidInput,
        NavBarCheckmark,
        ExportLayerPassword,
        Copy,
        LoadScreenIconGear,
        BoldArrowUp,
        BoldChevronRight,
        BoldChevronLeft,
        TrashCanAudio,
        TickerArrowLeft,
        TickerArrowRight,
        ConfigureWorkflow,
        ManageUsers,
        AssignRoles,
        AssignSections,
        CheckCircle,
        ReturnXIcon,
        PopUpWarning,
        StageCardArrow,
        DownloadError,
        SegmentNew,
        Unlock,
        DraftIconStage,
        ExportFinished,
        UnlockSegment,
        LockPlus,
        NoAssigned,
        Retry,
        AddFromComputer
    }
}