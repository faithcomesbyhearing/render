using Newtonsoft.Json;
using Render.Models.Scope;
using DynamicData;

namespace Render.Models.Sections.CommunityCheck
{
    /// <summary>
    /// The single CommunityTest entity stores all workflow Community Stages data per section.
    /// CommunityTestRepository retrieves the entity by the following criteria ParentId = draft.Id.
    /// Data for each Community stage screen must be filtered by the _flagsAllStages.Questions.StageIds and _retellsAllStages.StageId properties.
    /// </summary>
    public class CommunityTest : ScopeDomainEntity
    {
        private const int Version = 2;
        
        /// <summary>
        /// List of <see cref="Flag"/> objects added as part of the community check for the current draft.
        /// When the workflow has multiple community stages, Flag.Questions.StageIds property separates Flags for different Community stages.
        /// </summary>
        [JsonProperty("Flags")]
        private List<Flag> _flagsAllStages { get; } = new List<Flag>();
        
        /// <summary>
        /// Flags sorted by TimeMarker that are belong to all Community stages in the workflow.
        /// </summary>
        [JsonIgnore]
        public IOrderedEnumerable<Flag> FlagsAllStages => _flagsAllStages.OrderBy(x => x.TimeMarker);

        /// <summary>
        /// The Id of the <see cref="Draft"/> this community check was performed on.
        /// </summary>
        [JsonProperty("ParentId")]
        public Guid ParentId { get; }
        
        /// <summary>
        /// List of <see cref="CommunityRetell"/> performed for this round of community checks.
        /// When the workflow has multiple community stages, CommunityRetell.StageId property separates Flags for different Community stages.
        /// </summary>
        [JsonIgnore]
        private List<CommunityRetell> _retellsAllStages { get; } = new List<CommunityRetell>();
        
        // Use ICommunityTestService.GetCommunityTestForStage() to get Retells for a specific stage
        [JsonIgnore]
        public IReadOnlyList<CommunityRetell> RetellsAllStages => _retellsAllStages.AsReadOnly();

		/// <summary>
		/// List of <see cref="Response"/> performed for this round of community checks.
		/// When the workflow has multiple community stages, Response.StageId property separates Flags for different Community stages.
		/// </summary>
		[JsonIgnore]
		private List<Response> _responsesAllStages { get; } = new List<Response>();

		/// <summary>
		/// Use ICommunityTestService.GetCommunityTestForStage() to get Retells for a specific stage
		/// </summary>
		[JsonIgnore]
		public IReadOnlyList<Response> ResponsesAllStages => _responsesAllStages.AsReadOnly();

		public CommunityTest(Guid parentId, Guid scopeId, Guid projectId)
            : base(scopeId, projectId, Version)
        {
            ParentId = parentId;
        }

        public IEnumerable<Flag> GetFlags(Guid stageId) =>
           FlagsAllStages.Where(x => x.Questions.SelectMany(q => q.StageIds).Contains(stageId));

		public IEnumerable<Question> GetQuestions(Guid stageId) => GetFlags(stageId).SelectMany(x => x.Questions).
			Where(x => x.StageIds.Contains(stageId));

		public IEnumerable<Response> GetResponses(Guid stageId) => ResponsesAllStages.
			Where(x => x.StageId == stageId);

		public void AddRetells(List<CommunityRetell> retells)
        {
            _retellsAllStages.AddRange(retells);
        }
        
        public void AddRetell(CommunityRetell retell)
        {
            _retellsAllStages.Add(retell);
        }

        public void RemoveRetell(Guid retellId)
        {
            var retell = _retellsAllStages.FirstOrDefault(x => x.Id == retellId);
            _retellsAllStages.Remove(retell);
        }

		public void AddResponses(List<Response> responses)
		{
			_responsesAllStages.AddRange(responses);
		}

		public Flag GetCurrentFlag(Question current)
        {
            foreach (var flag in FlagsAllStages)
            {
                if (flag.Questions.IndexOf(current) >= 0)
                {
                    return flag;
                }
            }

            throw new ArgumentException("current question specified does not belong to any flag.");
        }
        
        public void CopyFlags(Guid fromStageId, Guid toStageId)
        {
            var questions = FlagsAllStages
                .SelectMany(x => x.Questions)
                .Where(x => x.StageIds.Contains(fromStageId));
            
            foreach (var question in questions)
            {
                question.AddStage(toStageId);
            }
        }

        public Flag AddFlag(double timeMarker)
        {
            var flag = new Flag(timeMarker);
            _flagsAllStages.Add(flag);
            return flag;
        }
        
        public Flag AddFlag(Flag flag)
        {
            _flagsAllStages.Add(flag);
            return flag;
        }

        public void RemoveFlag(Flag flag)
        {
            _flagsAllStages.Remove(flag);
        }
        
        public void RemoveFlags(IEnumerable<Flag> flags)
        {
            _flagsAllStages.Remove(flags);
        }
        
        public bool RemoveEmptyFlags()
        {
            if (FlagsAllStages == null || !FlagsAllStages.Any())
            {
                return false;
            }

            var flagsWithEmptyQuestionAudio = FlagsAllStages
                .Where(f => f.Questions?.Any(q => !q.QuestionAudio.HasAudio) ?? false).ToList();

            if (!flagsWithEmptyQuestionAudio.Any())
            {
                return false;
            }

            RemoveFlags(flagsWithEmptyQuestionAudio);

            return true;
        }
        
        public void RemoveFlagsThatDoNotBelongToAnyStage()
        {
            foreach (var flag in FlagsAllStages)
            {
                flag.Questions.RemoveAll(q => !q.StageIds.Any());
            }

            _flagsAllStages.RemoveAll(f => !f.Questions.Any());
        }

        public Question GetNextQuestion(Guid stageId, Question current)
        {
            if (stageId == default) throw new ArgumentException("Parameter should not be an empty GUID", nameof(stageId));
            if (current == null) throw new ArgumentNullException(nameof(current));

            var allQuestions = GetFlags(stageId)
                .SelectMany(f => f.Questions.Where(q => q.StageIds.Contains(stageId)))
                .ToList();
            
            var currentIndex = allQuestions.IndexOf(current);

            return currentIndex >= 0 && currentIndex + 1 < allQuestions.Count
                ? allQuestions.ElementAt(currentIndex + 1)
                : null;
        }
    }
}