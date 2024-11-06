using Render.Models.Sections;
using Render.Models.Workflow;

namespace Render.Services.InterpretationService;

public interface IInterpretationService
{
    Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
        Guid sectionId,
        Guid stageId,
        Guid stepId,
        StageTypes stageType);

    Task<Dictionary<Draft, List<Message>>> FindMessagesNeedingInterpretation(
        RenderWorkflow workflow,
        Guid sectionId,
        Guid stepId);

    Task<bool> IsNeedsInterpretation(
        RenderWorkflow workflow,
        Guid sectionId,
        Step step,
        List<Step> nextSteps);
}