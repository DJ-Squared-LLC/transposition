using Transposition.Api.Models;

namespace Transposition.Api.Services;

/// <summary>
/// Thread-safe in-memory queue of analysis jobs.
/// The background worker dequeues from this channel, ensuring work is processed
/// as soon as a thread is available (event-based, non-blocking producer side).
/// </summary>
public interface IAnalysisJobQueue
{
    /// <summary>Enqueue a job for processing. Returns the assigned job ID.</summary>
    Guid Enqueue(AnalysisJob job);

    /// <summary>Attempt to dequeue the next waiting job. Returns false when the queue is empty.</summary>
    bool TryDequeue(out AnalysisJob? job);

    /// <summary>Retrieve a job by ID regardless of its current status.</summary>
    AnalysisJob? GetById(Guid jobId);

    /// <summary>Signal that a new item has been enqueued.</summary>
    SemaphoreSlim WorkAvailable { get; }
}
