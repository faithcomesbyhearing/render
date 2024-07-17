using ReactiveUI.Maui;
using Render.Utilities;

namespace Render.Kernel
{
    public abstract class RenderComponentBase<T> :  ReactiveContentView<T>, IDisposable where T : class
    {
        private bool _disposed;

        protected IDisposable DisposableBindings;

        protected virtual void OnButtonClicked(object sender, EventArgs e)
        {
            var element = (Element)sender;
            var name = element.AutomationId;
            
            var properties = new Dictionary<string, string>
            {
                {"Button Name", name},
                {"ViewModel", nameof(ViewModel.GetType)}
            };
            LogInfo("Button Click", properties);
        }
        
        protected void LogInfo(string name, IDictionary<string, string> properties = null)
        {
            RenderLogger.LogInfo(name, properties);
        }
        
        protected void LogError(Exception exception, IDictionary<string, string> properties = null)
        {
            RenderLogger.LogError(exception, properties);
        }

        public void Dispose()
        {
            Dispose(true);
        }
        
        protected virtual void Dispose(bool disposing) 
        {
            if (_disposed) return;

            DisposableBindings?.Dispose();  
            DisposableBindings = null;

            // Don't forget dispose component ViewModel
            // from PageViewModel class
            ViewModel = null;
            _disposed = true;
        }
    }
}