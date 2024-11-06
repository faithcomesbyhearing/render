using Couchbase.Lite.Query;
using Render.TempFromVessel.Project;

namespace Render.Repositories.Kernel;

public class OffloadRepository : IOffloadRepository
{
    private readonly IDatabaseWrapper _renderLocalOnlyDatabaseWrapper;
    private readonly IDatabaseWrapper _renderDatabaseWrapper;
    private readonly IDatabaseWrapper _renderAudioDatabaseWrapper;

    public OffloadRepository(
        IDatabaseWrapper renderLocalOnlyDatabaseWrapper,
        IDatabaseWrapper renderDatabaseWrapper,
        IDatabaseWrapper renderAudioDatabaseWrapper)
    {
        _renderLocalOnlyDatabaseWrapper = renderLocalOnlyDatabaseWrapper;
        _renderDatabaseWrapper = renderDatabaseWrapper;
        _renderAudioDatabaseWrapper = renderAudioDatabaseWrapper;
    }

    public Task OffloadAsync(Guid projectId)
    {
       return Task.Run(() =>
        {
            PurgeRenderData(projectId);
            PurgeRenderAudioData(projectId);
        });
    }
    
    private void PurgeRenderData(Guid projectId)
    {
        var keysToRemove = new List<string>();
        _renderDatabaseWrapper.InBatch(() =>
        {
            using var result = QueryBuilder.Select(
                    SelectResult.Expression(Meta.ID))
                .From(_renderDatabaseWrapper.GetDataSource())
                .Where(Expression.Property("ProjectId").EqualTo(Expression.String(projectId.ToString()))
                    .And(Expression.Property("Type").NotEqualTo(Expression.String(nameof(Project)))));

            keysToRemove.AddRange(result.Execute().Select(row => row.GetString(0)));
        });

        foreach (var document in keysToRemove.Select(_renderDatabaseWrapper.GetDocument))
        {
            _renderDatabaseWrapper.Purge(document);
        }
        
        _renderDatabaseWrapper.CompactDatabase();
    }

    private void PurgeRenderAudioData(Guid projectId)
    {
        var keysToRemove = new List<string>();
        _renderAudioDatabaseWrapper.InBatch(() =>
        {
            using var result = QueryBuilder.Select(
                    SelectResult.Expression(Meta.ID))
                .From(_renderAudioDatabaseWrapper.GetDataSource())
                .Where(Expression.Property("ProjectId").EqualTo(Expression.String(projectId.ToString())));

            keysToRemove.AddRange(result.Execute().Select(row => row.GetString(0)));
        });

        foreach (var document in keysToRemove.Select(_renderAudioDatabaseWrapper.GetDocument))
        {
            _renderAudioDatabaseWrapper.Purge(document);
        }
        
        _renderAudioDatabaseWrapper.CompactDatabase();
    }
    
    public Task PurgeRenderLocalOnlyData()
    {
        return Task.Run(() =>
        {
            var keysToRemove = new List<string>();
            _renderLocalOnlyDatabaseWrapper.InBatch(() =>
            {
                using var result = QueryBuilder.Select(
                        SelectResult.Expression(Meta.ID))
                    .From(_renderLocalOnlyDatabaseWrapper.GetDataSource());

                keysToRemove.AddRange(result.Execute().Select(row => row.GetString(0)));
            });

            foreach (var document in keysToRemove.Select(_renderLocalOnlyDatabaseWrapper.GetDocument))
            {
                _renderLocalOnlyDatabaseWrapper.Purge(document);
            }
        
            _renderLocalOnlyDatabaseWrapper.CompactDatabase();
        });
    }
    
}