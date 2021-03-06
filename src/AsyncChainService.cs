﻿namespace LostTech.Checkpoint
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Serializes asynchronous operations into a chain,
    /// and allows to wait for the whole queue completion
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    public sealed class AsyncChainService
    {
        bool disposeInitiated;
        Task operationInProgress;

        /// <summary>
        /// Add an operation to the chain
        /// </summary>
        public void Chain(Func<Task> asyncOperation) => this.Chain(asyncOperation, true);

        /// <summary>
        /// Add an operation to the chain
        /// </summary>
        public void Chain(Func<Task> asyncOperation, bool captureContext)
        {
            if (this.disposeInitiated)
                throw new ObjectDisposedException("DisposeAsync was initiated");

            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));

            this.operationInProgress = this.AppendCheckpointInternal(asyncOperation, captureContext);
        }

        async Task AppendCheckpointInternal(Func<Task> saveOperation, bool captureContext)
        {
            var newFinalizationTask = this.operationInProgress ?? Task.FromResult(true);
            await newFinalizationTask.ConfigureAwait(captureContext);
            await saveOperation().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously disposes current instance
        /// </summary>
        public Task DisposeAsync()
        {
            this.disposeInitiated = true;

            return this.operationInProgress ?? Task.FromResult(true);
        }
    }
}
