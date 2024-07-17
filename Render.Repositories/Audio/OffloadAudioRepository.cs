using Couchbase.Lite.Query;
using Render.Repositories.Kernel;

namespace Render.Repositories.Audio;

public class OffloadAudioRepository(IDatabaseWrapper databaseWrapper) : IOffloadAudioRepository
{
    public async Task OffloadAudioForProject(Guid projectId)
    {
        await Task.Run(() =>
        {
            var keysToRemove = new List<string>();
            databaseWrapper.InBatch(() =>
            {
                var result = QueryBuilder.Select(
                        SelectResult.Expression(Meta.ID))
                    .From(databaseWrapper.GetDataSource())
                    .Where(Expression.Property("ProjectId").EqualTo(Expression.String(projectId.ToString())));
                
                keysToRemove.AddRange(result.Execute().Select(row => row.GetString(0)));
            });
            foreach (var document in keysToRemove.Select(databaseWrapper.GetDocument))
            {
                databaseWrapper.Purge(document);
            }
        });
    }
}