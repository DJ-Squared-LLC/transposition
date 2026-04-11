using Transposition.Api.Models;
using Transposition.Api.Services;

namespace Transposition.Tests;

public class InMemoryAnalysisJobQueueTests
{
    [Fact]
    public void Enqueue_AddsJobAndSignalsSemaphore()
    {
        var queue = new InMemoryAnalysisJobQueue();
        var job = new AnalysisJob();

        var id = queue.Enqueue(job);

        Assert.Equal(job.Id, id);
        // Semaphore should have been released once
        Assert.True(queue.WorkAvailable.Wait(TimeSpan.Zero));
    }

    [Fact]
    public void TryDequeue_AfterEnqueue_ReturnsJob()
    {
        var queue = new InMemoryAnalysisJobQueue();
        var job = new AnalysisJob();
        queue.Enqueue(job);
        queue.WorkAvailable.Wait(TimeSpan.Zero); // consume the signal

        bool dequeued = queue.TryDequeue(out var result);

        Assert.True(dequeued);
        Assert.NotNull(result);
        Assert.Equal(job.Id, result!.Id);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_ReturnsFalse()
    {
        var queue = new InMemoryAnalysisJobQueue();

        bool dequeued = queue.TryDequeue(out var result);

        Assert.False(dequeued);
        Assert.Null(result);
    }

    [Fact]
    public void GetById_ExistingJob_ReturnsJob()
    {
        var queue = new InMemoryAnalysisJobQueue();
        var job = new AnalysisJob();
        queue.Enqueue(job);

        var found = queue.GetById(job.Id);

        Assert.NotNull(found);
        Assert.Equal(job.Id, found!.Id);
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var queue = new InMemoryAnalysisJobQueue();

        var found = queue.GetById(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void Enqueue_NullJob_ThrowsArgumentNullException()
    {
        var queue = new InMemoryAnalysisJobQueue();
        Assert.Throws<ArgumentNullException>(() => queue.Enqueue(null!));
    }
}
