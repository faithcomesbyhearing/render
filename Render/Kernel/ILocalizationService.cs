namespace Render.Kernel
{
    public interface ILocalizationService
    {
        void SetLocalization(string culture);

        string GetCurrentLocalization();
    }
}