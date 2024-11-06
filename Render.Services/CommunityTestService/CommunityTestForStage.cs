using Render.Models.Sections.CommunityCheck;

namespace Render.Services
{
    public class CommunityTestForStage
    {
        public Guid StageId { get; }

        public IReadOnlyList<Flag> Flags { get; }

        public IReadOnlyList<CommunityRetell> Retells { get; }

		public IReadOnlyList<Response> Responses { get; }

		public CommunityTestForStage(Guid stageId, IReadOnlyList<Flag> flags, IReadOnlyList<CommunityRetell> retells)
		{
			StageId = stageId;
			Flags = flags;
			Retells = retells;
		}

		public CommunityTestForStage(Guid stageId, IReadOnlyList<Flag> flags, IReadOnlyList<CommunityRetell> retells, IReadOnlyList<Response> response)
        {
            StageId = stageId;
            Flags = flags;
            Retells = retells;
            Responses = response;
        }
    }
}