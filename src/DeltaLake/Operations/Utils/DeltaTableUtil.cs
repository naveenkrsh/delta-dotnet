using System.Reflection;
using System.Text.Json;
using DeltaLake.Log.Actions;
using DeltaLake.Operations.Extensions;
using DeltaLake.Operations.Models;
using Action = DeltaLake.Log.Actions.Action;

namespace DeltaLake.Operations.Utils {
    internal static class DeltaTableUtil {
        public static JsonElement CreateCommitInfo(OperationEnum operation) {
            string clientVersion = $"DeltaIO-{Assembly.GetExecutingAssembly().GetName().Version}";
            var commitInfoModel = new CommitInfoModel(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                clientVersion,
                operation.ToDeltaOperationString(),
                new Dictionary<string, string>(),
                true,
                Guid.NewGuid().ToString(),
                new Dictionary<string, string>()
            );

            string jsonString = JsonSerializer.Serialize(commitInfoModel, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement;
        }

        public static void MapActionsToCommitLines(List<Action> actions, List<CommitLine> commitLines) {
            foreach(Action action in actions) {
                commitLines.Add(new CommitLine() { Add = (AddFile)action });
            }
        }
    }
}