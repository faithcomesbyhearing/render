using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Workflow
{
    public class Step : DomainEntity
    {
	    /// <summary>
	    /// The ordering of the steps.
	    /// </summary>
	    public enum Ordering
	    {
		    Sequential,
		    Parallel,
		    Optional
	    }

	    [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("Order")]
        public Ordering Order { get; }
        
        /// <summary>
        /// An enum that differentiates specific steps for the purpose of telling navigation which page to go to
        /// when it's a work step and what icon to display when it's a workflow entry step
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("RenderStepType")]
        public RenderStepTypes RenderStepType { get; }

        /// <summary>
        /// The name of the role that can accomplish this step
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("Role")]
        public Roles Role { get; private set; }

        /// <summary>
		/// The steps to perform on the section
		/// </summary>
        [JsonProperty("Steps")]
        protected List<Step> Steps { get; set; } = new List<Step>();

        [JsonIgnore]
        public bool IsLeaf => !Steps.Any();
        
        [JsonProperty("StepSettings")]
        public WorkflowSettings StepSettings { get; set; } = new WorkflowSettings(Setting.IsActive);

		public Step(
            RenderStepTypes renderStepType = RenderStepTypes.NotSpecial, 
            Roles role = Roles.None, 
            Ordering order = Ordering.Sequential, 
            WorkflowSettings settings = null) : base(0)
	    {
		    Order = order;
            RenderStepType = renderStepType;
            Role = role;
            if (settings != null)
            {
                StepSettings = settings;
            }
        }
        
        public virtual void AddStep(Step renderStep)
        {
            Steps.Add(renderStep);
        }

        public virtual void AddSteps(IEnumerable<Step> renderSteps)
        {
            Steps.AddRange(renderSteps);
        }

        public IReadOnlyList<Step> GetSubSteps()
        {
            return Steps.AsReadOnly();
        }
        
        public bool IsActive() => StepSettings.GetSetting(SettingType.IsActive);

        /// <summary>
        /// Recursively finds a step by its guid
        /// </summary>
        /// <returns>Yes, this is the step you were looking for.</returns>
        public Step GetStep(Guid stepId)
        {
            if (Id == stepId)
            {
                return this;
            }

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
            if (IsLeaf && (!onlyActive || IsActive())
                       && RenderStepType != RenderStepTypes.HoldingTank && RenderStepType != RenderStepTypes.NotSpecial) 
            {
                steps.Add(this);
            }
            else if (!IsLeaf)
            {
                foreach (var step in Steps)
                {
                    steps.AddRange(step.GetAllWorkflowEntrySteps(onlyActive));
                }
            }

            return steps;
        }

        /// <summary>
        /// Recursively find the work step(s) that are next in the workflow after the current step
        /// </summary>
        public List<Step> GetNextSteps(Guid currentStepId, ref bool matched)
        {
            var previouslyMatched = matched;
            var steps = new List<Step>();
            foreach (var step in Steps)
            {
                if (step.IsLeaf)
                {
                    if (matched && step.IsActive())
                    {
                        steps.Add(step);
                        if (Order != Ordering.Parallel)
                        {
                            return steps;
                        }
                    }

                    if (step.Id == currentStepId)
                    {
                        matched = true;
                        if (Order == Ordering.Parallel)
                        {
                            return steps;
                        }
                    }
                }
                else if (step.IsActive())
                {
                    steps.AddRange(step.GetNextSteps(currentStepId, ref matched));

                    //If this isn't a parallel step and we've found steps already, we want to break out to the next level,
                    //otherwise if this is a parallel step and one of its children is the current step, then we want to
                    //also break out to the next level
                    if (steps.Any() && Order != Ordering.Parallel || matched && !previouslyMatched && Order == Ordering.Parallel)
                    {
                        return steps;
                    }
                }
            }

            return steps;
        }
    }
}
