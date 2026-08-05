using System.Threading.Channels;

namespace TrainTicketReservationSystem
    .BackgroundJobs.Reports
{
  public sealed class ReportJobQueue :
      IReportJobQueue
  {
    private readonly Channel<Guid> _queue;

    public ReportJobQueue(
        IConfiguration configuration)
    {
      var capacity =
          configuration.GetValue<int?>(
              "Reports:QueueCapacity")
          ?? 20;

      if (capacity <= 0)
      {
        throw new InvalidOperationException(
            "Reports:QueueCapacity must be greater than zero.");
      }

      var options =
          new BoundedChannelOptions(capacity)
          {
            /*
             * When QueueAsync is used and the queue is full,
             * the writer waits asynchronously until space
             * becomes available.
             */
            FullMode =
                  BoundedChannelFullMode.Wait,

            /*
             * Only one background report worker will read
             * from this queue initially.
             */
            SingleReader = true,

            /*
             * Several HTTP requests may add report jobs
             * at the same time.
             */
            SingleWriter = false,

            /*
             * Avoid running continuations directly inside
             * the channel operation.
             */
            AllowSynchronousContinuations = false
          };

      _queue =
          Channel.CreateBounded<Guid>(options);
    }

    public bool TryQueue(Guid reportJobId)
    {
      ValidateReportJobId(reportJobId);

      return _queue.Writer.TryWrite(
          reportJobId);
    }

    public async ValueTask QueueAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default)
    {
      ValidateReportJobId(reportJobId);

      await _queue.Writer.WriteAsync(
          reportJobId,
          cancellationToken);
    }

    public ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken)
    {
      return _queue.Reader.ReadAsync(
          cancellationToken);
    }

    private static void ValidateReportJobId(
        Guid reportJobId)
    {
      if (reportJobId == Guid.Empty)
      {
        throw new ArgumentException(
            "A valid report job ID is required.",
            nameof(reportJobId));
      }
    }
  }
}