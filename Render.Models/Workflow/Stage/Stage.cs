using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow.Stage
{
    public class Stage : DomainEntity
    {
        [Reactive]
        [JsonProperty("Name")]
        public string Name { get; private set; }
        
        [JsonProperty("StageType")]
        public StageTypes StageType { get; private set; }
        
        /// <summary>
        /// In the case where a workflow has multiple of the same type of stage, this number will be a positive number
        /// in the order in which it sits in the workflow for that stage type. If there is only one stage of this type,
        /// this will be set to the default of 0.
        /// </summary>
        [JsonProperty("StageNumber")]
        public int StageNumber { get; set; }
        
        [JsonIgnore] public static string DraftingDefaultStageName = "Draft";
        [JsonIgnore] public static string PeerCheckDefaultStageName = "Peer Check";
        [JsonIgnore] public static string CommunityTestDefaultStageName = "Community Test";
        [JsonIgnore] public static string ConsultantCheckDefaultStageName = "Consultant Check";
        [JsonIgnore] public static string ConsultantApprovalDefaultStageName = "Consultant Approval";

        public IReadOnlyList<Roles> GetRoles()
        {
            var roles = new List<Roles>();
            foreach (var step in Steps.Where(step => step.Role != Roles.None && !roles.Contains(step.Role)))
            {
                roles.Add(step.Role);
            }
            return roles.AsReadOnly();
        }

        /// <summary>
        /// The steps to perform on the section
        /// </summary>
        [JsonProperty("ReviewPreparationSteps")]
        protected List<Step> ReviewPreparationSteps { get; private set; }
        	= new List<Step>();
        
        [JsonProperty("ReviewSteps")]
        protected List<Step> ReviewSteps { get; private set; }
            = new List<Step>();
        
        [JsonProperty("RevisePreparationSteps")]
        protected List<Step> RevisePreparationSteps { get; private set; }
            = new List<Step>();
        
        [JsonProperty("ReviseStep")]
        public Step ReviseStep { get; private set; }
        
        [JsonProperty("StageSettings")]
        public WorkflowSettings StageSettings { get; set; } = new WorkflowSettings();
        
        [JsonProperty("State")]
        public StageState State { get; private set; }
        
        [JsonIgnore]
        public bool IsActive => State == StageState.Active;

        [JsonIgnore]
        public bool IsRemoved => State == StageState.RemoveWork;

        /// <summary>
        /// Indicates the stage was removed, but any in-process work must be completed
        /// </summary>
        [JsonIgnore]
        public bool IsCompleteWork => State == StageState.CompleteWork;
        
        [JsonIgnore]
        public virtual List<Step> Steps
        {
            get
            {
                var steps = new List<Step>();
                //Ignore the hidden steps if they're empty
                if (!ReviewPreparationSteps.First().GetSubSteps().Any())
                {
                    for (var i = 2; i < ReviewPreparationSteps.Count; i++)
                    {
                        steps.Add(ReviewPreparationSteps[i]);
                    }
                }
                else
                {
                    steps.AddRange(ReviewPreparationSteps);
                }
                steps.AddRange(ReviewSteps);
                steps.AddRange(RevisePreparationSteps);
                if(ReviseStep != null)
                    steps.Add(ReviseStep);
                return steps;
            }
        }

        //The constructor creates two hidden substeps in order to organize its parallel processes
        public Stage(StageTypes stageType = StageTypes.Generic) : base(1)
        {
            StageType = stageType;
            ReviewPreparationSteps.Add(new Step(order: Step.Ordering.Parallel));
            ReviewPreparationSteps.Add(new Step(RenderStepTypes.HoldingTank));
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public void AddParallelReviewPreparationStep(Step step)
        {
            ReviewPreparationSteps.First().AddStep(step);
        }

        public void AddSequentialReviewPreparationStep(Step step)
        {
            ReviewPreparationSteps.Add(step);
        }
        
        public void AddReviewStep(Step step)
        {
            ReviewSteps.Add(step);
        }
        
        public void AddRevisePreparationStep(Step step)
        {
            RevisePreparationSteps.Add(step);
        }
        
        public void SetReviseStep(Step step)
        {
            ReviseStep = step;
        }

        public IReadOnlyList<Step> GetSubSteps()
        {
            return Steps.AsReadOnly();
        }
        
        /// <summary>
        /// Recursively finds a step by its guid
        /// </summary>
        /// <returns>Yes, this is the step you were looking for.</returns>
        public Step GetStep(Guid stepId)
        {
            foreach (var step in Steps)
            {
                var output = step.GetStep(stepId);
                if (output != null)
                {
                    return output;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Recursively find all multi steps that are workflow entry points that are active
        /// </summary>
        public List<Step> GetAllWorkflowEntrySteps(bool onlyActive = true)
        {
            var steps = new List<Step>();
            foreach (var step in Steps)
            {
                steps.AddRange(step.GetAllWorkflowEntrySteps(onlyActive));
            }

            return steps;
        }
        
        /// <summary>
        /// Recursively find the work step(s) that are next in the workflow after the current step
        /// </summary>
        public List<Step> GetNextSteps(Guid currentStepId, ref bool matched)
        {
            var steps = new List<Step>();
            foreach (var step in Steps)
            {
                if (step.IsLeaf)
                {
                    if (matched && step.IsActive())
                    {
                        steps.Add(step);
                        return steps;
                    }
        
                    if (step.Id == currentStepId)
                    {
                        matched = true;
                    }
                }
                else if (step.IsActive())
                {
                    steps.AddRange(step.GetNextSteps(currentStepId, ref matched));
        
                    if (steps.Any())
                    {
                        return steps;
                    }
                }
            }
        
            return steps;
        }

        public void SetState(StageState state)
        {
            State = state;
        }
    }
}