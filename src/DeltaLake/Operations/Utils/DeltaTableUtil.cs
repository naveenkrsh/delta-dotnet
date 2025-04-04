using System.Reflection;
using System.Text.Json;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Models;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations.Utils {
    internal static class DeltaTableUtil {
        /// <summary>
        /// Creates a commit info JSON element for Delta table operations.
        /// </summary>
        /// <param name="operation">The operation type (e.g., CREATE, UPDATE).</param>
        /// <returns>A JSON element representing the commit info.</returns>
        public static JsonElement CreateCommitInfo(OperationEnum operation) {
            string clientVersion = GetClientVersion();
            CommitInfoModel commitInfoModel = BuildCommitInfoModel(operation, clientVersion);

            return SerializeToJsonElement(commitInfoModel);
        }

        /// <summary>
        /// Retrieves the client version from the executing assembly.
        /// </summary>
        /// <returns>The client version string.</returns>
        private static string GetClientVersion() {
            return $"DeltaIO-{Assembly.GetExecutingAssembly().GetName().Version}";
        }

        /// <summary>
        /// Builds the commit info model.
        /// </summary>
        /// <param name="operation">The operation type.</param>
        /// <param name="clientVersion">The client version string.</param>
        /// <returns>A populated CommitInfoModel instance.</returns>
        private static CommitInfoModel BuildCommitInfoModel(OperationEnum operation, string clientVersion) {
            return new CommitInfoModel(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                clientVersion,
                operation.ToDeltaOperationString(),
                new Dictionary<string, string>(),
                true,
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()
            );
        }

        /// <summary>
        /// Serializes an object to a JSON element.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A JSON element representing the serialized object.</returns>
        private static JsonElement SerializeToJsonElement(object obj) {
            string jsonString = JsonSerializer.Serialize(obj, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement;
        }
    }
}