using Stowage;

namespace DeltaLake.Operations.Storage
{
  
    public interface IDeltaStorage {
        /// <summary>
        /// Gets the underlying storage implementation.
        /// </summary>
        IFileStorage UnderlyingStorage { get; }

        /// <summary>
        /// Gets the cached storage implementation.
        /// </summary>
        ICachedStorage CachedStorage { get; }

        /// <summary>
        /// Gets the root location of the Delta table.
        /// </summary>
        IOPath Location { get; }
    }

    public class DeltaStorage : IDeltaStorage {
        public DeltaStorage(IFileStorage underlyingStorage, IOPath location) {
            UnderlyingStorage = underlyingStorage;
            Location = location;
            CachedStorage = Files.Of.MemoryCacheStorage(underlyingStorage, location.ToString());
        }
        public IFileStorage UnderlyingStorage { get; }
        public ICachedStorage CachedStorage { get; }
        public IOPath Location {
            get;
        }
    }
}