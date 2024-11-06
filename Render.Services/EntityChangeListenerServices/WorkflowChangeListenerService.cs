using Render.Models.Sections;
using Render.Models.Workflow;
using Render.Repositories.Kernel;

namespace Render.Services.EntityChangeListenerServices;

public class WorkflowChangeListenerService : IEntityChangeListenerService
{
    private readonly IDocumentChangeListener _documentChangeListener;

    private Func<Guid, Task> _actionOnDocumentChanged;

    public WorkflowChangeListenerService(
        IDocumentChangeListener documentChangeListener,
        RenderWorkflow renderWorkflow,
        List<WorkflowStatus> workflowStatusList)
    {
        _documentChangeListener = documentChangeListener;
        RenderWorkflow = renderWorkflow;
        WorkflowStatusList = workflowStatusList;
    }

    private RenderWorkflow RenderWorkflow { get; set; }
    private List<WorkflowStatus> WorkflowStatusList { get; set; }

    public void InitializeListener(Func<Guid, Task> actionOnDocumentChanged)
    {
        if (RenderWorkflow == null) return;

        _actionOnDocumentChanged = actionOnDocumentChanged;
        _documentChangeListener.MonitorDocumentByField<RenderWorkflow>(nameof(RenderWorkflow.Id),
            RenderWorkflow.Id.ToString(), _actionOnDocumentChanged);

        foreach (var workflowStatus in WorkflowStatusList)
            _documentChangeListener.MonitorDocumentByField<WorkflowStatus>(nameof(WorkflowStatus.Id),
                workflowStatus.Id.ToString(), _actionOnDocumentChanged);
    }

    public void ResetListeners()
    {
        if (RenderWorkflow == null) return;

        var renderWorkflowId = RenderWorkflow.Id.ToString();
        _documentChangeListener.StopMonitoringDocumentByField<RenderWorkflow>(nameof(RenderWorkflow.Id),
            renderWorkflowId);
        _documentChangeListener.MonitorDocumentByField<RenderWorkflow>(nameof(RenderWorkflow.Id),
            renderWorkflowId, _actionOnDocumentChanged);
        foreach (var workflowStatus in WorkflowStatusList)
        {
            _documentChangeListener?.StopMonitoringDocumentByField<WorkflowStatus>(nameof(WorkflowStatus.Id), workflowStatus.Id.ToString());
            _documentChangeListener?.MonitorDocumentByField<WorkflowStatus>(nameof(WorkflowStatus.Id),
                workflowStatus.Id.ToString(), _actionOnDocumentChanged);
        }
    }

    public void RemoveListeners()
    {
        if (RenderWorkflow is null || WorkflowStatusList is null) return;

        _documentChangeListener?.StopMonitoringDocumentByField<RenderWorkflow>(nameof(RenderWorkflow.Id),
            RenderWorkflow.Id.ToString());

        foreach (var workflowStatus in WorkflowStatusList)
            _documentChangeListener?.StopMonitoringDocumentByField<WorkflowStatus>(nameof(WorkflowStatus.Id),
                workflowStatus.Id.ToString());
    }

    public void Dispose()
    {
        RemoveListeners();
        _documentChangeListener?.Dispose();

        WorkflowStatusList = null;
        RenderWorkflow = null;
    }
}