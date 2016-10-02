namespace LostTech.Checkpoint
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class CheckpointService
    {
        volatile bool disposeInitiated;
        Task saveInProgress;
        Func<Task> requestedSave;
        public void Checkpoint(Func<Task> saveOperation)
        {
            if (disposeInitiated)
                throw new ObjectDisposedException("DisposeAsync was initiated");
            if (saveOperation == null)
                throw new ArgumentNullException(nameof(saveOperation));

            Func<Task> previousRequestedSave = Interlocked.Exchange(ref requestedSave, saveOperation);
            if (previousRequestedSave != null)
                // TODO: NO! This is wrong! Previous requested save might still be in progress.
                RunRequestedSave();
        }
        async void RunRequestedSave()
        {
            Func<Task> saveRequest = Interlocked.Exchange(ref requestedSave, null);
            while (saveRequest != null) {
                await saveRequest().ConfigureAwait(false);
                Volatile.Write(ref saveInProgress, saveRequest());
                saveRequest = Interlocked.Exchange(ref requestedSave, null);
            }
        }

        public async Task DispooseAsync()
        {
            this.disposeInitiated = true;

            Task currentSaveTask = Interlocked.Exchange(ref this.saveInProgress, null);
            // TODO: how to ensure, that both save task and reqested save are never going to be non-null again?
            while (currentSaveTask != null)
                await currentSaveTask.ConfigureAwait(false);
        }
    }
}
