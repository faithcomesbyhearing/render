using Render.Kernel;
using Render.Resources.Localization;
using System.Globalization;

namespace Render.Platforms.Kernel
{
    public class LocalizationService : ILocalizationService
    {
        private const string DefaultCulture = "en";

        private static string _culture = DefaultCulture;

        public void SetLocalization(string culture)
        {
            var cultureInfo = string.IsNullOrEmpty(culture) ? new CultureInfo(DefaultCulture) : new CultureInfo(culture);
            AppResources.Culture = cultureInfo;
            
            _culture = cultureInfo.TwoLetterISOLanguageName;
        }

        public string GetCurrentLocalization()
        {
            return _culture;
        }
    }
}