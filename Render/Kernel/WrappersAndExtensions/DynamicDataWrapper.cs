using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;

namespace Render.Kernel.WrappersAndExtensions
{
    /// <summary>
    /// This is a wrapper for ReactiveUI's Dynamic Data for lists. This is only for a simple list binding.
    /// If more complex list filtering/managing is needed, this won't be very usable.
    /// </summary>
    public class DynamicDataWrapper<TDisposable> : IDisposable where TDisposable : class, IDisposable
    {
        private readonly ReadOnlyObservableCollection<TDisposable> _items;

        private SourceList<TDisposable> SourceList { get; } = new ();
        private IObservable<IChangeSet<TDisposable>> _observable;
        private IDisposable Disposables { get; }
        
        public ReadOnlyObservableCollection<TDisposable> Items => _items;
        public IEnumerable<TDisposable> SourceItems => SourceList.Items;
        public IObservable<IChangeSet<TDisposable>> Observable => _observable;
        
        public DynamicDataWrapper(SortExpressionComparer<TDisposable> comparer = null)
        {
            _observable = SourceList
                .Connect();

            if(comparer is null)
            {
                Disposables = _observable
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Bind(out _items)
                                .Subscribe();
            }
            else
            {
                Disposables = _observable
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Sort(comparer)
                                .Bind(out _items)
                                .Subscribe();
            }
        }

        public void AddRange(List<TDisposable> itemsToAdd)
        {
            SourceList.AddRange(itemsToAdd);
        }

        public void Add(TDisposable itemToAdd)
        {
            SourceList.Add(itemToAdd);
        }

        public void Remove(TDisposable itemToRemove)
        {
            SourceList.Remove(itemToRemove);
        }

        public void Dispose()
        {
            SourceList?.Dispose();
            Disposables.Dispose();

            foreach (var item in Items)
            {
                item.Dispose();
            }

            SourceList?.Clear();

        }

        public void Clear()
        {
            SourceList.Clear();
        }
    }
}