using Render.Models.Sections;
using Render.Models.Sections.CommunityCheck;

namespace Render.Repositories.SectionRepository
{
    public interface ICommunityTestRepository : IDisposable
    {
        Task<CommunityTest> GetCommunityTestForDraftAsync(Draft draft);

        Task<CommunityTest> GetExistingCommunityTestForDraftAsync(Draft draft);

        Task SaveCommunityTestAsync(CommunityTest test);

        Task Purge(Guid id);
    }
}