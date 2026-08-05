namespace TrainTicketReservationSystem
    .BackgroundJobs.Reports
{
  public interface IReportJobQueue
  {
    /// <summary>
    /// Attempts to add a report job immediately.
    /// Returns false when the bounded queue is full.
    /// </summary>
    bool TryQueue(Guid reportJobId);

    /// <summary>
    /// Waits asynchronously until space becomes available,
    /// then adds the report job.
    /// </summary>
    ValueTask QueueAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits asynchronously until a report job is available.
    /// Used by the background worker.
    /// </summary>
    ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken);
  }
}