using DynamicData;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Render.Models.Workflow.Stage;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow
{
    /// <summary>
    /// This is a multi-faceted look at what a "team" is in a translation. The team is mostly referring to who is translating
    /// sections. The team is also referring to everyone involved in the translation process for a section. In the UI,
    /// when someone clicks the "Add Team" button, they are adding a team to do translation work, but they are also adding
    /// references to other users who will be doing things like checks and back translations as part of the entire translation
    /// process. This encompasses both uses of the word.
    /// </summary>
    public class Team : DomainEntity
    {
        [JsonProperty("TranslatorId")]
        public Guid TranslatorId { get; private  set; }

        [JsonProperty("TeamNumber")]
        public int TeamNumber { get; private set; }
        
        [JsonIgnore]
        public IReadOnlyList<WorkflowAssignment> WorkflowAssignments => _workflowAssignments.AsReadOnly();
        
        [JsonProperty("WorkflowAssignments")]
        private List<WorkflowAssignment> _workflowAssignments { get; } = new List<WorkflowAssignment>();
        
        [JsonIgnore]
        public IReadOnlyList<SectionAssignment> SectionAssignments => _sectionAssignments.AsReadOnly();

        [Reactive]
        [JsonProperty("SectionAssignments")]
        private List<SectionAssignment> _sectionAssignments { get; set; } = new List<SectionAssignment>();
        
        [JsonIgnore]
        [Reactive]
        public int SectionAssignmentCount { get; private set; }
        
        public Team(int teamNumber) : base(Version)
        {
            TeamNumber = teamNumber;
            this.WhenAnyValue(x => x._sectionAssignments.Count).Subscribe(i => SectionAssignmentCount = i);
        }

        public void RemoveTranslator()
        {
            TranslatorId = Guid.Empty;
        }

        public void UpdateTranslator(Guid translatorId)
        {
            TranslatorId = translatorId;
        }

        public void AddAssignment(WorkflowAssignment workflowAssignment)
        {
            // If there is an existing workflow assignment with the same role and stage ID at this point, remove it
            // and add the new one in case the user ID is changed
            // currently we have 1 user per team, so we delete all existing assignments by stage and role
            RemoveAssignment(workflowAssignment.StageId, workflowAssignment.Role);

            _workflowAssignments.Add(workflowAssignment);
        }

        public void RemoveAssignment(Guid stageId, Roles role)
        {
            var assignments = _workflowAssignments.Where(x => x.StageId == stageId && x.Role == role).ToList();
            if (assignments.Count != 0)
            {
                _workflowAssignments.Remove(assignments);
            }
        }

        public void AddSectionAssignment(Guid sectionId, int priority)
        {
            var assignment = _sectionAssignments.FirstOrDefault(x => x.SectionId == sectionId);
            if (assignment != null)
            {
                _sectionAssignments.Replace(assignment, new SectionAssignment(sectionId, priority));
            }
            else
            {
                _sectionAssignments.Add(new SectionAssignment(sectionId, priority));
            }
            SectionAssignmentCount = _sectionAssignments.Count;
        }

        public void RemoveSectionAssignment(Guid sectionId)
        {
            var assignment = _sectionAssignments.Find(x => x.SectionId == sectionId);
            _sectionAssignments.Remove(assignment);
            SectionAssignmentCount = _sectionAssignments.Count;
        }

        /// <summary>
        /// Get the assignment for the given stage and role (e.g. Peer Check 1, Reviewer).
        /// If this returns null, then the assignment has not been made yet for that stage/role combination.
        /// </summary>
        /// <param name="stageId">The Id of the specific stage of the workflow</param>
        /// <param name="role">The Id of the role in question (Reviewer, Back Translator, Consultant, etc.)</param>
        /// <returns>The <see cref="WorkflowAssignment"/> object for the given stage and role id's.</returns>
        public WorkflowAssignment GetWorkflowAssignmentForStageAndRole(Guid stageId, Roles role)
        {
            return _workflowAssignments.FirstOrDefault(x => x.StageId == stageId && x.Role == role);
        }

        public WorkflowAssignment GetWorkflowAssignmentForRole(Roles role)
        {
            return _workflowAssignments.FirstOrDefault(x => x.Role == role);
        }

        public List<WorkflowAssignment> GetWorkflowAssignmentsForUser(Guid userId)
        {
            return _workflowAssignments.Where(x => x.UserId == userId).ToList();
        }
        
        private const int Version = 1;

        public void IncreasePriority(SectionAssignment sectionAssignment)
        {
            var newAssignment = sectionAssignment.IncreasePriorityBy1();
            _sectionAssignments.Replace(sectionAssignment, newAssignment);
        }
    }
}