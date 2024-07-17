namespace Render.Models.Workflow
{
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
        HoldingTank
	}

	public enum StageTypes
	{
		Generic,
		Drafting,
		PeerCheck,
		CommunityTest,
		ConsultantCheck,
		ConsultantApproval,
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