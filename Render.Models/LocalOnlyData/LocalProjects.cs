using Newtonsoft.Json;
using Render.TempFromVessel.Kernel;

namespace Render.Models.LocalOnlyData
{
    public class LocalProjects : DomainEntity
    {
        [JsonProperty("Projects")]
        private List<LocalProject> Projects { get; set; } = new();

        public void AddDownloadedProject(Guid projectId)
        {
            ChangeState(projectId, DownloadState.Finished);
        }
        
        public void AddPartiallyDownloadedProject(Guid projectId)
        {
            ChangeState(projectId, DownloadState.FinishedPartially);
        }

        private void ChangeState(Guid projectId, DownloadState state)
        {
            var existingProject = Projects.FirstOrDefault(x => x.ProjectId == projectId);
            if (existingProject != null)
            {
                if (existingProject.State == state) return;
                existingProject.State = state;
            }
            else
            {
                Projects.Add(new LocalProject { ProjectId = projectId, State = state });
            }
        }
        
        public DownloadState GetState(Guid projectId)
        {
            var existingProject = Projects.FirstOrDefault(x => x.ProjectId == projectId);
            if (existingProject != null)
            {
                return existingProject.State;
            }

            return DownloadState.NotStarted;
        }

        public void Download(Guid projectId)
        {
            ChangeState(projectId, DownloadState.Downloading);
        }
        
        public void Offload(Guid projectId)
        {
            ChangeState(projectId, DownloadState.Offloading);
        }
        
        public void Cancel(Guid projectId)
        {
            ChangeState(projectId, DownloadState.Canceling);
        }
        
        public void AddProjects(List<Guid> projectIds)
        {
            Projects.AddRange(projectIds.Select(x=>new LocalProject {ProjectId = x, State = DownloadState.Finished}));
        }

        public void SetNotStartedState(Guid projectId)
        {
            var projectToRemove = Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (projectToRemove != null) projectToRemove.State = DownloadState.NotStarted;
        }

        public bool CheckForDownloadedProject(Guid projectId = default)
        {
            if (projectId == Guid.Empty)
            {
                return Projects.Count > 0;
            }
            return Projects.Any(x => x.ProjectId == projectId && x.State == DownloadState.Finished);
        }

        public List<Guid> GetProjectIds()
        {
            return Projects.Select(x=>x.ProjectId).ToList();
        }
        
        public List<LocalProject> GetProjects()
        {
            return Projects;
        }

        public LocalProject GetProject(Guid projectId)
        {
            return Projects.FirstOrDefault(x => x.ProjectId == projectId);
        }
        
        public IEnumerable<LocalProject> GetDownloadedProjects()
        {
            return Projects.Where(x => x.State == DownloadState.Finished);
        }
        
        public void RemoveNotStartedProjectsFromMachine()
        {
            Projects.RemoveAll(x => x.State == DownloadState.NotStarted);
        }

        public void RemoveProjectFromMachine(Guid projectId)
        {
            Projects.RemoveAll(x => x.ProjectId == projectId);
        }
        
        public LocalProjects() : base(0)
        {
        }
    }
}