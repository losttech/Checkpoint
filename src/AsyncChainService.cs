namespace LostTech.Checkpoint
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Serializes asynchronous operations into a chain,
    /// and allows to wait for the whole queue completion
    /// </summary>
    /// <threadsafety static="true" instance="false" />
    public sealed class AsyncChainService
    {
        bool disposeInitiated;
        Task? operationInProgress;

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

        /// <summary>
        /// Occurs when one of the tasks in the chain fails
        /// </summary>
        public event EventHandler<UnobservedTaskExceptionEventArgs>? TaskException;
        /// <summary>
        /// An aggregate of all exceptions thrown by the tasks in the chain so far,
        /// that were not caught by the <see cref="TaskException"/> event.
        /// </summary>
        public AggregateException? FatalErrors { get; private set; }

        async Task AppendCheckpointInternal(Func<Task> saveOperation, bool captureContext)
        {
            var newFinalizationTask = this.operationInProgress ?? Task.FromResult(true);
            await newFinalizationTask.ConfigureAwait(captureContext);
            try {
                await saveOperation().ConfigureAwait(false);
            } catch (Exception e) {
                var exceptionArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(e));
                this.TaskException?.Invoke(this, exceptionArgs);
                if (!exceptionArgs.Observed) {
                    this.FatalErrors = this.FatalErrors is null
                        ? exceptionArgs.Exception
                        : new AggregateException(this.FatalErrors.InnerExceptions.Concat(new[] { e }));
                }
            }
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
