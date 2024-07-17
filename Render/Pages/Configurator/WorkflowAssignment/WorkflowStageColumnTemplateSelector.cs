using Render.Models.Workflow;
using Render.Pages.Configurator.WorkflowAssignment.Stages;

namespace Render.Pages.Configurator.WorkflowAssignment;

public class WorkflowStageColumnTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate _draftingTemplate = new(typeof (WorkflowDraftStageColumn));
    private readonly DataTemplate _peerCheckTemplate = new(typeof (WorkflowPeerCheckStageColumn));
    private readonly DataTemplate _communityCheckTemplate = new(typeof (WorkflowCommunityCheckStageColumn));
    private readonly DataTemplate _consultantCheckTemplate = new(typeof (WorkflowConsultantCheckStageColumn));
    private readonly DataTemplate _consultantApprovalTemplate = new(typeof (WorkflowConsultantApprovalStageColumn));

    protected override DataTemplate OnSelectTemplate (object item, BindableObject container)
    {
        if (item is WorkflowStageColumnViewModel workflowStageColumnViewModel)
        {
            var stageType = workflowStageColumnViewModel.StageType;
            
            switch (stageType)
            {
                case StageTypes.Generic:
                    break;
                case StageTypes.Drafting:
                    return _draftingTemplate;
                case StageTypes.PeerCheck:
                    return _peerCheckTemplate;
                case StageTypes.CommunityTest:
                    return _communityCheckTemplate;
                case StageTypes.ConsultantCheck:
                    return _consultantCheckTemplate;
                case StageTypes.ConsultantApproval:
                    return _consultantApprovalTemplate;
            }
        }

        return default;
    }
}