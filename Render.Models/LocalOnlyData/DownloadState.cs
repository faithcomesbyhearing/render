namespace Render.Models.LocalOnlyData
{
    public enum DownloadState
    {
        NotStarted,
        Downloading,
        Offloading,
        Finished,
        Canceling,
        FinishedPartially,
        Exporting,
        ExportDone
    }
}