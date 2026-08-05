namespace TrainTicketReservationSystem.Services.Bookings
{
  public enum BookingFailureType
  {
    None,
    Validation,
    NotFound,
    Conflict,
    DependencyUnavailable
  }

  public sealed class BookingServiceResult<T>
  {
    private BookingServiceResult(
        T? value,
        BookingFailureType failureType,
        IReadOnlyList<string> errors)
    {
      Value = value;
      FailureType = failureType;
      Errors = errors;
    }

    public T? Value { get; }

    public BookingFailureType FailureType { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool Succeeded =>
        FailureType == BookingFailureType.None;

    public static BookingServiceResult<T> Success(
        T value)
    {
      return new BookingServiceResult<T>(
          value,
          BookingFailureType.None,
          Array.Empty<string>());
    }

    public static BookingServiceResult<T> Failure(
        BookingFailureType failureType,
        params string[] errors)
    {
      return new BookingServiceResult<T>(
          default,
          failureType,
          errors);
    }
  }
}