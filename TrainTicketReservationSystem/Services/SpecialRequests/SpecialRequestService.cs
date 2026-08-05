using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Models.SpecialRequests;
using TrainTicketReservationSystem.Repositories.SpecialRequests;

namespace TrainTicketReservationSystem.Services.SpecialRequests
{
  public class SpecialRequestService : ISpecialRequestService
  {
    private static readonly string[] AllowedStatuses =
        {
            "Pending",
            "Approved",
            "Completed",
            "Rejected"
        };

    private readonly ApplicationDBContext _dbContext;
    private readonly ISpecialRequestRepository _repository;
    private readonly ILogger<SpecialRequestService> _logger;

    public SpecialRequestService(ApplicationDBContext dbContext, ISpecialRequestRepository repository, ILogger<SpecialRequestService> logger)
    {
      _dbContext = dbContext;
      _repository = repository;
      _logger = logger;
    }

    public async Task<IReadOnlyList<SpecialRequestResponseDto>> GetAllRequests(CancellationToken cancellationToken = default)
    {
      var records = await _repository.GetAllRequests(cancellationToken);

      return records
          .Select(ToResponseDto)
          .ToList();
    }

    public async Task<SpecialRequestResponseDto?> GetRequestById(Guid specialRequestId, CancellationToken cancellationToken = default)
    {
      var record = await _repository.GetRequestById(specialRequestId, cancellationToken);

      return record is null
          ? null
          : ToResponseDto(record);
    }

    public async Task<IReadOnlyList<SpecialRequestResponseDto>> GetRequestByBookingId(Guid bookingId, CancellationToken cancellationToken = default)
    {
      var records = await _repository.GetRequestByBookingId(bookingId, cancellationToken);

      return records
          .Select(ToResponseDto)
          .ToList();
    }

    public async Task<SpecialRequestResponseDto?> CreateRequest(CreateSpecialRequestDto dto, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(dto);

      // Thecks the SQL Booking table. No XML record is created unless the related booking exists.
      var bookingExists = await _dbContext
          .Set<Booking>()
          .AsNoTracking()
          .AnyAsync(booking => booking.BookingId == dto.BookingId, cancellationToken);

      if (!bookingExists)
      {
        _logger.LogWarning("Special request was rejected because booking {BookingId} does not exist.", dto.BookingId);

        return null;
      }

      var record = new SpecialRequestRecord
      {
        SpecialRequestId = Guid.NewGuid(),
        BookingId = dto.BookingId,
        RequestType = dto.RequestType.Trim(),
        Description = dto.Description.Trim(),
        Status = "Pending",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = null
      };

      await _repository.AddRequest(record, cancellationToken);

      return ToResponseDto(record);
    }

    public async Task<bool> UpdateRequest(Guid specialRequestId, UpdateSpecialRequestDto dto, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(dto);
      var existingRecord = await _repository.GetRequestById(specialRequestId, cancellationToken);

      if (existingRecord is null)
      {
        return false;
      }

      var normalizedStatus = NormalizeStatus(dto.Status);
      existingRecord.RequestType = dto.RequestType.Trim();
      existingRecord.Description = dto.Description.Trim();
      existingRecord.Status = normalizedStatus;
      existingRecord.UpdatedAtUtc = DateTimeOffset.UtcNow;

      return await _repository.UpdateRequest(existingRecord, cancellationToken);
    }

    public async Task<bool> DeleteRequest(Guid specialRequestId, CancellationToken cancellationToken = default)
    {
      return await _repository.DeleteRequest(specialRequestId, cancellationToken);
    }

    private static string NormalizeStatus(string status)
    {
      var matchingStatus = AllowedStatuses.FirstOrDefault(allowedStatus => string.Equals(allowedStatus, status.Trim(), StringComparison.OrdinalIgnoreCase));

      if (matchingStatus is null)
      {
        throw new ArgumentException("Status must be Pending, Approved, Completed or Rejected.", nameof(status));
      }

      return matchingStatus;
    }

    private static SpecialRequestResponseDto ToResponseDto(SpecialRequestRecord record)
    {
      return new SpecialRequestResponseDto
      {
        SpecialRequestId = record.SpecialRequestId,
        BookingId = record.BookingId,
        RequestType = record.RequestType,
        Description = record.Description,
        Status = record.Status,
        CreatedAtUtc = record.CreatedAtUtc,
        UpdatedAtUtc = record.UpdatedAtUtc
      };
    }
  }
}
