using System.Collections.Concurrent;
using Transposition.Api.Models;

namespace Transposition.Api.Services;

/// <inheritdoc/>
public class InMemoryAnalysisJobQueue : IAnalysisJobQueue
{
    private readonly ConcurrentQueue<Guid> _pendingIds = new();
    private readonly ConcurrentDictionary<Guid, AnalysisJob> _store = new();

    /// <inheritdoc/>
    public SemaphoreSlim WorkAvailable { get; } = new(0);

    /// <inheritdoc/>
    public Guid Enqueue(AnalysisJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        _store[job.Id] = job;
        _pendingIds.Enqueue(job.Id);
        WorkAvailable.Release();
        return job.Id;
    }

    /// <inheritdoc/>
    public bool TryDequeue(out AnalysisJob? job)
    {
        if (_pendingIds.TryDequeue(out var id) && _store.TryGetValue(id, out job))
            return true;

        job = null;
        return false;
    }

    /// <inheritdoc/>
    public AnalysisJob? GetById(Guid jobId) =>
        _store.TryGetValue(jobId, out var job) ? job : null;
}
