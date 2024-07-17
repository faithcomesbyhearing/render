using Newtonsoft.Json;
using ReactiveUI.Fody.Helpers;
using Render.Models.Audio;
using Render.TempFromVessel.Kernel;

namespace Render.Models.Sections.CommunityCheck
{
    public class Flag : DomainEntity
    {
        [JsonProperty("Questions")]
        public List<Question> Questions { get; set; } = new List<Question>();

        [Reactive]
        [JsonIgnore]
        public int QuestionCount { get; set; }

        [JsonProperty("TimeMarker")]
        public double TimeMarker { get; set; }
        
        public Flag(double timeMarker)
         :base(Version)
        {
            TimeMarker = timeMarker;
        }

        public Question AddQuestion(Guid stageId, Guid scopeId, Guid projectId)
        {
            var lastQuestion = Questions.LastOrDefault();
            if(lastQuestion != null && (lastQuestion.QuestionAudio == null || !lastQuestion.QuestionAudio.HasAudio)) 
                return null;
            var questionAudio = new NotableAudio(scopeId, projectId, Id);
            var question = new Question(stageId)
            {
                QuestionAudio = questionAudio,
                QuestionAudioId = questionAudio.Id
            };
            Questions.Add(question);
			UpdateQuestionCountByStage(stageId);
			return question;
        }
        
        private const int Version = 1;

        public void RemoveEmptyQuestions()
        {
            Questions.RemoveAll(x => x.QuestionAudio == null);
        }

		/// <summary>
		/// For the scenario where we have multiple community test stages
		/// we need to consider removing the stage id for the question
		/// </summary>
		/// <param name="question"></param>
		/// <param name="stageId"></param>
		/// <returns></returns>
		public bool RemoveQuestion(Question question, Guid stageId)
		{
			var questionToRemoveStageIdFrom = Questions.SingleOrDefault(x => x.Id == question.Id);
			if (questionToRemoveStageIdFrom == null)
			{
				return false;
			}
			questionToRemoveStageIdFrom.RemoveFromStage(stageId);
			QuestionCount = GetQuestionsForStage(stageId).Count;
			if (!questionToRemoveStageIdFrom.StageIds.Any())
			{
				Questions.Remove(question); 
                return true;
			}

			return false;
		}

		public List<Question> GetQuestionsForStage(Guid stageId)
		{
			return Questions.Where(x => x.StageIds.Contains(stageId)).ToList();
		}

		public void UpdateQuestionCountByStage(Guid stageId)
		{
			QuestionCount = Questions.Count(x => x.StageIds.Contains(stageId));
		}
	}
}