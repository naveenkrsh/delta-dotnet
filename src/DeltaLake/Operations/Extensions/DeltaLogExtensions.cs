using DeltaLake.Log;
using DeltaLake.Log.Actions;

namespace DeltaLake.Operations.Extensions
{
    /// <summary>
    /// Provides extension methods for optimized Delta log operations.
    /// </summary>
    internal static class DeltaLogExtensions
    {
        /// <summary>
        /// Gets a lazy enumeration of log commits to optimize memory usage.
        /// </summary>
        public static async IAsyncEnumerable<LogCommit> GetHistoryLazyAsync(this DeltaLog log)
        {
            IReadOnlyCollection<LogCommit> entries = await log.ReadHistoryAsync();
            foreach (LogCommit entry in entries)
            {
                yield return entry;
            }
        }

        /// <summary>
        /// Lazily reads all actions from the log history.
        /// </summary>
        public static async IAsyncEnumerable<DeltaLake.Log.Actions.Action> GetActionsLazyAsync(this DeltaLog log)
        {
            await foreach (LogCommit commit in log.GetHistoryLazyAsync())
            {
                foreach (DeltaLake.Log.Actions.Action action in commit.Actions)
                {
                    yield return action;
                }
            }
        }

        /// <summary>
        /// Gets the most recent metadata action from the log.
        /// </summary>
        public static async Task<Metadata?> GetLatestMetadataAsync(this DeltaLog log)
        {
            await foreach (DeltaLake.Log.Actions.Action action in log.GetActionsLazyAsync())
            {
                if (action is Metadata metadata)
                {
                    return metadata;
                }
            }
            return null;
        }
    }
}