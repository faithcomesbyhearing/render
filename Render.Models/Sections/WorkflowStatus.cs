﻿using System;
using Newtonsoft.Json;
using Render.Models.Scope;
using Render.Models.Workflow;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections
{
    public class WorkflowStatus : ScopeDomainEntity, IAggregateRoot
    {
        /// <summary>
        /// The step in the workflow the section is currently in
        /// </summary>
        [JsonProperty("CurrentStepId")]
        public Guid CurrentStepId { get; private set; }

        /// <summary>
        /// The step type of the current step
        /// </summary>
        [JsonProperty("CurrentStepType")]
        public RenderStepTypes CurrentStepType { get; private set; }

        /// <summary>
        /// The stage in the workflow the section is currently in
        /// </summary>
        [JsonProperty("CurrentStageId")]
        public Guid CurrentStageId { get; private set; }

        /// <summary>
        /// The workflow that the section is currently traversing
        /// </summary>
        [JsonProperty("WorkflowId")]
        public Guid WorkflowId { get; }

        /// <summary>
        /// The user that generated this workflow status
        /// </summary>
        [JsonProperty("UserId")]
        public Guid UserId { get; private set; }

        /// <summary>
        /// Whether or not the section's work for this step is finished. This is only set to true if there are
        /// no further steps in the workflow to push the section forward to.
        /// </summary>
        [JsonProperty("IsCompleted")]
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// The section this object corresponds to.
        /// </summary>
        [JsonProperty("ParentSectionId")]
        public Guid ParentSectionId { get; }

        [JsonProperty("SectionHasNotesToInterpret")]
        public bool SectionHasNotesToInterpret { get; private set; }
        
        [JsonProperty("HasNewMessages")]
        public bool HasNewMessages { get; private set; }
        
        [JsonProperty("HasNewDrafts")]
        public bool HasNewDrafts { get; private set; }

        public WorkflowStatus(Guid parentSectionId, Guid workflowId, Guid projectId, Guid stepId, Guid scopeId,
            Guid currentStageId, RenderStepTypes currentStepType) : base(scopeId, projectId, 1)
        {
            ParentSectionId = parentSectionId;
            WorkflowId = workflowId;
            CurrentStepId = stepId;
            CurrentStepType = currentStepType;
            CurrentStageId = currentStageId;
        }

        /// <summary>
        /// Updates the step the section is currently in.
        /// </summary>
        public void MoveSectionToNewStep(Guid stepId, Guid stageId, RenderStepTypes currentStepType)
        {
            IsCompleted = false;
            CurrentStepId = stepId;
            CurrentStageId = stageId;
            CurrentStepType = currentStepType;
        }

        public void SetUserId(Guid userId)
        {
            UserId = userId;
        }

        public void SetHasNotesToInterpret(bool hasNotesToInterpret)
        {
            SectionHasNotesToInterpret = hasNotesToInterpret;
        }

        public void MarkAsCompleted()
        {
            IsCompleted = true;
        }

        public void SetHasNewMessages(bool hasNewMessages)
        {
            HasNewMessages = hasNewMessages;
        }
        
        public void SetHasNewDraft(bool hasNewMessages)
        {
            HasNewDrafts = hasNewMessages;
        }
    }
}