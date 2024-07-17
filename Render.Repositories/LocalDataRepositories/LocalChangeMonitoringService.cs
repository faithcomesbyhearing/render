using Couchbase.Lite;
using Render.TempFromVessel.Document_Extensions;

namespace Render.Repositories.LocalDataRepositories
{
    public class LocalChangeMonitoringService : IDisposable
    {
        private bool _isDisposing;

        private string Bucket { get; set; }

        private readonly Database _database;

        private readonly Dictionary<string, ListenerToken> _monitorDictionary;

        private readonly ReaderWriterLockSlim _lock;

        private ListenerToken DatabaseListenerToken { get; set; }

        private bool DatabaseListenerActive { get; set; }

        private readonly List<(string fieldName, string fieldValue, Action action, Type type)> _databaseListeners =
            new List<(string fieldName, string fieldValue, Action action, Type type)>();

        private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1);

        protected LocalChangeMonitoringService(string bucket, string databasePath=null)
        {
            Bucket = bucket;
            var configuration = databasePath is null ? null : new DatabaseConfiguration()
            {
                Directory = databasePath,
            };

            _monitorDictionary = new Dictionary<string, ListenerToken>();
            _database = new Database(Bucket, configuration);
            _lock = new ReaderWriterLockSlim();
        }

        public async Task MonitorDocumentByIdAsync<T>(Guid id, Action<object, Guid> action)
        {
            await Task.Run(() =>
            {
                RunSafeWithLock(() =>
                {
                    var key = ConvertIdToKey<T>(id);
                    if (_monitorDictionary.ContainsKey(key))
                    {
                        var token = _monitorDictionary[key];
                        _monitorDictionary.Remove(key);
                        _database.RemoveChangeListener(token);
                    }

                    var newToken = _database.AddDocumentChangeListener(
                        key, (o, e) =>
                        {
                            var document = e.Database.GetDocument(e.DocumentID);
                            var obj = document.ToObject<T>();
                            action.Invoke(obj, Guid.Parse(e.DocumentID.Split(':')[1]));
                        });
                    _monitorDictionary.Add(key, newToken);
                }, ClearListeners);
            });
        }

        public void RemoveDocumentById<T>(Guid id)
        {
            RunSafeWithLock(() =>
            {
                var key = ConvertIdToKey<T>(id);
                if (_monitorDictionary.Count > 0 && _monitorDictionary.ContainsKey(key))
                {
                    var token = _monitorDictionary[key];
                    _monitorDictionary.Remove(key);
                    _database.RemoveChangeListener(token);
                }
            });
        }

        private string ConvertIdToKey<T>(Guid id)
        {
            //Generates a key with the following format 'script::00000000-0000-0000-0000-000000000000'
            return $"{typeof(T).Name.ToLower()}::{id}";
        }

        public void CancelService()
        {
            Dispose();
        }

        public void MonitorDocumentByField<T>(string fieldName, string fieldValue, Action action)
        {
            RunSafeWithLock(() =>
            {
                if (_databaseListeners.Any(
                    x => x.fieldName == fieldName && x.fieldValue == fieldValue && x.action == action))
                {
                    _databaseListeners.Remove(
                        _databaseListeners.Find(
                            x => x.fieldName == fieldName && x.fieldValue == fieldValue && x.action == action));
                }

                _databaseListeners.Add((fieldName, fieldValue, action, typeof(T)));
                if (!DatabaseListenerActive)
                {
                    DatabaseListenerToken = _database.AddChangeListener(OnDatabaseChangeEvent);
                    DatabaseListenerActive = true;
                }
            }, ClearListeners);
        }

        public void StopMonitoringDocumentByField<T>(string fieldName, string fieldValue)
        {
            RunSafeWithLock(() =>
            {
                var listener =
                    _databaseListeners.FirstOrDefault(x => x.fieldName == fieldName && x.fieldValue == fieldValue);
                if (listener != default)
                {
                    _databaseListeners.Remove(listener);
                }

                if (_databaseListeners.Count == 0)
                {
                    _database.RemoveChangeListener(DatabaseListenerToken);
                    DatabaseListenerActive = false;
                }
            });
        }

        private async void OnDatabaseChangeEvent(object sender, DatabaseChangedEventArgs e)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                List<string> documents = new List<string>();
                documents.AddRange(e.DocumentIDs);
                foreach (var documentId in documents)
                {
                    var document = _database?.GetDocument(documentId);
                    var documentKeyArray = documentId.Split(':');
                    Guid.TryParse(documentKeyArray[2], out var documentIdGuid);
                    if (document == null) // if delete
                    {
                        var listeners = _databaseListeners.ToList();
                        foreach (var listener in listeners)
                        {
                            if (documentKeyArray[0] == listener.type.Name.ToLower() && documentIdGuid.ToString() == listener.fieldValue)
                            {
                                listener.action.Invoke();
                            }
                        }
                    }
                }
            }
            finally
            {
                SemaphoreSlim.Release();
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
            foreach (var item in _monitorDictionary)
            {
                _database.RemoveChangeListener(item.Value);
            }

            _database.RemoveChangeListener(DatabaseListenerToken);
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