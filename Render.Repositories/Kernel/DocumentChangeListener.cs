using Couchbase.Lite;
using Render.Interfaces;
using Render.Models.Sections;
using Render.Models.Users;
using Render.Models.Workflow;

namespace Render.Repositories.Kernel
{
    public class DocumentChangeListener : IDocumentChangeListener
    {
        private readonly IRenderLogger _renderLogger;
        private bool _isDisposing;
        private string Bucket { get; set; }

        private readonly Database _database;
        
        private readonly ReaderWriterLockSlim _lock;

        private ListenerToken DatabaseListenerToken { get; set; }

        private bool DatabaseListenerActive { get; set; }

        private readonly List<(string fieldName, string fieldValue, Type type)> _databaseListeners = [];

        private SemaphoreSlim SemaphoreSlim { get; } = new(1);

        private Func<Guid, Task> ActionOnDocumentChanged { get; set; }

        public DocumentChangeListener(IRenderLogger renderLogger, string bucket, string databasePath = null)
        {
            _renderLogger = renderLogger;
            Bucket = bucket;
            var configuration = databasePath is null
                ? null
                : new DatabaseConfiguration()
                {
                    Directory = databasePath,
                };
            
            _database = new Database(Bucket, configuration);
            _lock = new ReaderWriterLockSlim();
        }
        
        public void MonitorDocumentByField<T>(string fieldName, string fieldValue, Func<Guid,Task> onDatabaseChanged)
        {
            RunSafeWithLock(() =>
            {
                ActionOnDocumentChanged ??= onDatabaseChanged;
                
                if (_databaseListeners.Any(
                        x => x.fieldName == fieldName && x.fieldValue == fieldValue && x.type == typeof(T)))
                {
                    _databaseListeners.Remove(
                        _databaseListeners.Find(
                            x => x.fieldName == fieldName && x.fieldValue == fieldValue && x.type == typeof(T)));
                }

                _databaseListeners.Add((fieldName, fieldValue, typeof(T)));
                if (!DatabaseListenerActive)
                {
                    DatabaseListenerToken = _database.GetDefaultCollection()!.AddChangeListener(OnDatabaseChangeEvent);
                    DatabaseListenerActive = true;
                }
            }, ClearListeners);
        }

        public void StopMonitoringDocumentByField<T>(string fieldName, string fieldValue)
        {
            RunSafeWithLock(() =>
            {
                var listener =
                    _databaseListeners.FirstOrDefault(x => x.fieldName == fieldName && x.fieldValue == fieldValue && x.type == typeof(T));
                if (listener != default)
                {
                    _databaseListeners.Remove(listener);
                }

                if (_databaseListeners.Count == 0)
                {
                    _database.GetDefaultCollection()!.RemoveChangeListener(DatabaseListenerToken);
                    DatabaseListenerActive = false;
                }
            });
        }
        
        private async void OnDatabaseChangeEvent(object sender, CollectionChangedEventArgs e)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                var documents = new List<string>();
                documents.AddRange(e.DocumentIDs);
                foreach (var documentId in documents)
                {
                    var document = _database?.GetDefaultCollection()?.GetDocument(documentId);
                    var documentKeyArray = documentId.Split(':');
                    Guid.TryParse(documentKeyArray[2], out var documentIdGuid);

                    switch (document?["Type"].String)
                    {
                        case nameof(RenderWorkflow):
                            await CallActionIfTheDocumentInSubscriptions(documentKeyArray, documentIdGuid);
                            break;
                        case nameof(WorkflowStatus):
                            await CallActionIfTheDocumentInSubscriptions(documentKeyArray, documentIdGuid);
                            break;
                        case nameof(User):
                            await CallActionIfTheDocumentInSubscriptions(documentKeyArray, documentIdGuid);
                            break;
                    }
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        
        private async Task CallActionIfTheDocumentInSubscriptions(string[] documentKeyArray, Guid documentIdGuid)
        {
            if (_databaseListeners.Any(listener => documentKeyArray[0].Equals(listener.type.Name, StringComparison.CurrentCultureIgnoreCase)
                                                   && documentIdGuid.ToString() == listener.fieldValue))
            {
                await ActionOnDocumentChanged.Invoke(documentIdGuid);
            }
        }
        
        // need to prevent crash, because sometimes multiple click the Home button causes '_lock' to be disposed
        private void RunSafeWithLock(Action action, Action finallyActionIfDisposing = default)
        {
            if (_isDisposing)
            {
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                if (_isDisposing)
                {
                    return;
                }

                action();
            }
            finally
            {
                if (_isDisposing && finallyActionIfDisposing is not null)
                {
                    finallyActionIfDisposing();
                }

                if (!_isDisposing)
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        private void ClearListeners()
        {
            _database.GetDefaultCollection()!.RemoveChangeListener(DatabaseListenerToken);
            _databaseListeners.Clear();
        }

        public void Dispose()
        {
            _isDisposing = true;

            ClearListeners();
            _lock.Dispose();
            _database.Dispose();
        }
    }
}