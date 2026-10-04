using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameEngine.Services;

namespace GameEngine.Persistence;

public static class ResearchSync
{
    // Acknowledge only after both response rows and the game snapshot are durable.
    // Resolving the latest checkpoint avoids losing turns committed during upload.
    public static async Task<int> FlushAsync(ClientServices services, Func<Checkpoint> latest,
        Action<Checkpoint> save, CancellationToken token)
    {
        var items = latest().Outbox.ToArray();
        foreach (var item in items.Where(item => item.kind == "turn"))
            await services.SendEventAsync(item, token);
        var ids = items.Select(item => item.id).ToArray();
        var snapshot = latest().Snapshot();
        snapshot.Acknowledge(ids);
        await services.SyncCheckpointAsync(snapshot, token);
        var current = latest();
        current.Acknowledge(ids);
        save(current);
        return items.Count(item => item.kind == "turn");
    }
}
