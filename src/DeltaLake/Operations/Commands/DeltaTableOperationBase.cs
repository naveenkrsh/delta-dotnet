using DeltaLake.Operations.Storage;

namespace DeltaLake.Operations.Commands
{
    /// <summary>
    /// Base class for all Delta table operations providing common functionality.
    /// </summary>
    public abstract class DeltaTableOperationBase : IDeltaTableOperation
    {
        protected readonly IDeltaStorage Storage;
        private bool _isValidated;

        protected DeltaTableOperationBase(IDeltaStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_isValidated)
            {
                await ValidateAsync(cancellationToken);
            }
            await ExecuteCoreAsync(cancellationToken);
        }

        public async Task ValidateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await ValidateCoreAsync(cancellationToken);
            _isValidated = true;
        }

        /// <summary>
        /// Core implementation of the operation execution.
        /// </summary>
        protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Core implementation of the operation validation.
        /// </summary>
        protected abstract Task ValidateCoreAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}