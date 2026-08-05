using TrainTicketReservationSystem.Models.SpecialRequests;

namespace TrainTicketReservationSystem.Repositories.SpecialRequests
{
  public interface ISpecialRequestRepository
  {
    /// <summary>
    /// Returns every special request stored in the XML file.
    /// </summary>
    Task<IReadOnlyList<SpecialRequestRecord>> GetAllRequests(CancellationToken cancellationToken);

    /// <summary>
    /// Returns one special request using its unique ID.
    /// Returns null when the record does not exist.
    /// </summary>
    Task<SpecialRequestRecord?> GetRequestById(Guid specialRequestId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all special requests belonging to one SQL booking.
    /// </summary>
    Task<IReadOnlyList<SpecialRequestRecord>> GetRequestByBookingId(Guid bookingId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new special request to the XML file.
    /// </summary>
    Task AddRequest(SpecialRequestRecord specialRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing special request.
    /// Returns false when the record cannot be found.
    /// </summary>
    Task<bool> UpdateRequest(SpecialRequestRecord specialRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a special request using its unique ID.
    /// Returns false when the record cannot be found.
    /// </summary>
    Task<bool> DeleteRequest(Guid specialRequestId, CancellationToken cancellationToken);
  }
}
