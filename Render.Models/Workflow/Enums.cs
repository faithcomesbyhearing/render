using System.ComponentModel;

namespace Render.Models.Workflow
{
	// Do not change the RenderStepTypes enum values order because sometimes values are stored as an Integer in DB.
	public enum RenderStepTypes
	{
        NotSpecial,
        Draft,
        PeerCheck,
        PeerRevise,
        CommunitySetup,
        CommunityTest,
        CommunityRevise,
        BackTranslate,
        InterpretToConsultant,
        InterpretToTranslator,
        Transcribe,
        ConsultantCheck,
        ConsultantRevise,
        ConsultantApproval,
        HoldingTank,
        Unknown, // A step that is created in the newest app version, but is not supported by the current app version
        CIT
	}

	public enum StageTypes
	{
        [Description("Generic")]
        Generic,
        [Description("Draft")]
        Drafting,
        [Description("Peer Check")]
        PeerCheck,
        [Description("Community Test")]
        CommunityTest,
        [Description("Consultant Check")]
        ConsultantCheck,
        [Description("Consultant Approval")]
        ConsultantApproval,
        [Description("Approved")]
        Approved
    }
	
	public enum StageState
	{
		Active, 
		CompleteWork,
		RemoveWork
	}

	public enum Roles
	{
		None,
		Drafting,
		Review,
		BackTranslate,
		BackTranslate2,
		NoteTranslate,
		Transcribe,
		Transcribe2,
		Consultant,
		Approval
	}
}