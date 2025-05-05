namespace DeltaLake.Operations.Commands
{
    /// <summary>
    /// Defines the contract for all Delta table operations.
    /// </summary>
    public interface IDeltaTableOperation
    {
        /// <summary>
        /// Executes the Delta table operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Validates if the operation can be executed.
        /// </summary>
        /// <returns>A task representing the asynchronous validation operation.</returns>
        Task ValidateAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}