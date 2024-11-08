﻿namespace Render.Services.SyncService
{
    public interface ILocalProjectDownloader
    {
        event Action<LocalReplicationResult> DownloadFinished;

        bool BeginActiveLocalReplicationOfProject(Device device);

        void CancelActiveLocalReplicationOfProject(Device device);

        void Dispose();
    }
}