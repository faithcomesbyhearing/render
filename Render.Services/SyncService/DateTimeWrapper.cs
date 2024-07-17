namespace Render.Services.SyncService
{
    public class DateTimeWrapper : IDateTimeWrapper
    {
        public TimeSpan GetTimeOfDay()
        {
            return DateTime.Now.TimeOfDay;
        }
    }
}