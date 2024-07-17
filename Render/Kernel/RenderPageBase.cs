using ReactiveUI.Maui;
using Render.Resources;
using Render.Utilities;

namespace Render.Kernel
{
    public abstract class RenderPageBase<T> : ReactiveContentPage<T>, IDisposable where T: class, IDisposable
    {
        private bool _disposed;

        protected RenderPageBase()
        {
            SetValue(NavigationPage.HasNavigationBarProperty, false);
            SetValue(NavigationPage.HasBackButtonProperty, false);
            SetValue(StyleProperty, ResourceExtensions.GetResourceValue("PrimaryPage"));
        }

        protected virtual void OnButtonClicked(object sender, EventArgs e)
        {
            var element = (Element)sender;
            var name = element.AutomationId;

            var properties = new Dictionary<string, string>
            {
                {"Button Name", name},
                {"ViewModel", nameof(ViewModel)}
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

        /// <summary>
        /// Override this method for inheritors that need to release resources.
        /// Do it carefully!
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) 
        {
            if (_disposed) return;

            ViewModel?.Dispose();
            ViewModel = null;

            _disposed = true;
        }
    }
}