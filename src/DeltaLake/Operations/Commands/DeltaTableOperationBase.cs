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

        public async Task ExecuteAsync()
        {
            if (!_isValidated)
            {
                await ValidateAsync();
            }
            await ExecuteCoreAsync();
        }

        public async Task ValidateAsync()
        {
            await ValidateCoreAsync();
            _isValidated = true;
        }

        /// <summary>
        /// Core implementation of the operation execution.
        /// </summary>
        protected abstract Task ExecuteCoreAsync();

        /// <summary>
        /// Core implementation of the operation validation.
        /// </summary>
        protected abstract Task ValidateCoreAsync();
    }
}