using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Repositories.SectionRepository;
using Render.Services.WorkflowService;

namespace Render.Services.InterpretationService;

public class InterpretationService : IInterpretationService
{
	private readonly IWorkflowService _workflowService;
	private readonly ISectionRepository _sectionRepository;

	public InterpretationService(
		IWorkflowService workflowService,
		ISectionRepository sectionRepository)
	{
		_workflowService = workflowService;
		_sectionRepository = sectionRepository;
	}

	public async Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
		Guid sectionId,
		Guid stageId,
		Guid stepId,
		StageTypes stageType)
	{
		var dictionary = new Dictionary<Draft, List<Message>>();
		var section = await _sectionRepository.GetSectionWithDraftsAsync(sectionId, true, true);
		if (section is null)
		{
			return dictionary;
		}

		var projectWorkflow = _workflowService.ProjectWorkflow;
		var backTranslateSecondStep = projectWorkflow.GetAllActiveWorkflowEntrySteps()
			.SingleOrDefault(step => step.Role is Roles.BackTranslate2);
		var isTwoStepBackTranslateEnabled = backTranslateSecondStep != null &&
											(backTranslateSecondStep.StepSettings.GetSetting(SettingType
												 .DoRetellBackTranslate) ||
											 backTranslateSecondStep.StepSettings.GetSetting(SettingType
												 .DoSegmentBackTranslate));

		var nextStep = projectWorkflow.GetNextSteps(stepId).FirstOrDefault();
		var currentStep = projectWorkflow.GetStep(stepId);
		var isInterpretToTranslator =
			currentStep?.RenderStepType == RenderStepTypes.InterpretToTranslator ||
			nextStep?.RenderStepType == RenderStepTypes.InterpretToTranslator;

		var needToShowDraftNotesOnConsultantCheck = _workflowService.NeedToShowDraftNotesOnConsultantCheck(projectWorkflow, stageId, section);

        foreach (var passage in section.Passages)
		{
			//Check for messages on draft
			if (stageType == StageTypes.Drafting || stageType == StageTypes.ConsultantCheck &&
                needToShowDraftNotesOnConsultantCheck || isInterpretToTranslator)
			{
				FindMessagesInDraft(passage.CurrentDraftAudio, dictionary);
			}

			//Check for messages on retell
			if (isTwoStepBackTranslateEnabled)
			{
				if (passage.CurrentDraftAudio.RetellBackTranslationAudio != null)
				{
					FindMessagesInDraft(passage.CurrentDraftAudio.RetellBackTranslationAudio, dictionary);
				}

				//Check for messages on segments
				foreach (var segment in passage.CurrentDraftAudio.SegmentBackTranslationAudios)
				{
					FindMessagesInDraft(segment, dictionary);
				}
			}
		}

		return dictionary;
	}

	public async Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
		RenderWorkflow workflow,
		Guid sectionId,
		Guid stepId)
	{
		var stage = workflow.GetStage(stepId);

		return await FindMessagesNeedingInterpretation(sectionId, stage.Id, stepId, stage.StageType);
	}

	public async Task<bool> IsNeedsInterpretation(
		RenderWorkflow workflow,
		Guid sectionId,
		Step step,
		List<Step> nextSteps)
	{
		var messageToInterpretExists =
			(await FindMessagesNeedingInterpretation(workflow, sectionId, step.Id)).Count is not 0;
		if (messageToInterpretExists is false)
		{
			return false;
		}

		var btSteps = workflow.GetAllActiveWorkflowEntrySteps()
			.Where(x => x.RenderStepType == RenderStepTypes.BackTranslate).ToList();

		// Outgoing notes from "Consultant Check" sub-stage should trigger note interpretation
		// if note interpret is turned on, regardless if there is no BT step or single BT step.
		var currentStep = workflow.GetStep(step.Id);
		var needFindMessages = workflow.GetAllActiveWorkflowEntrySteps()
			.Any(x => x.RenderStepType == RenderStepTypes.InterpretToConsultant
					  || x.RenderStepType == RenderStepTypes.InterpretToTranslator);
		if (currentStep.RenderStepType == RenderStepTypes.ConsultantCheck && btSteps.Count <= 1)
		{
			return needFindMessages && messageToInterpretExists;
		}

		if (currentStep.RenderStepType == RenderStepTypes.Draft
			&& nextSteps.Any()
			&& !btSteps.Any()
			&& needFindMessages)
		{
			var nextStage = workflow.GetStage(nextSteps.First().Id);

			if (nextStage.StageType == StageTypes.ConsultantCheck)
			{
				return messageToInterpretExists;
			}
		}

		return btSteps.Count >= 1 && messageToInterpretExists;
	}

	private static void FindMessagesInDraft(Draft draft, Dictionary<Draft, List<Message>> dictionary)
	{
		foreach (var message in draft.Conversations.SelectMany(conversation => conversation.Messages))
		{
			if (message.NeedsInterpretation)
			{
				if (!dictionary.ContainsKey(draft))
				{
					dictionary.Add(draft, new List<Message>());
				}

				dictionary[draft].Add(message);
			}
		}
	}
}