using Microsoft.AspNetCore.Mvc;
using TrainTicketReservationSystem.Models.SpecialRequests;
using TrainTicketReservationSystem.Services.SpecialRequests;

namespace TrainTicketReservationSystem.Controllers
{
  [ApiController]
  [Route("api/special-requests")]
  public class SpecialRequestsController : ControllerBase
  {
    private readonly ISpecialRequestService _service;

    public SpecialRequestsController(ISpecialRequestService service)
    {
      _service = service;
    }

    // GET: api/special-requests
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecialRequestResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
      var requests = await _service.GetAllRequests(cancellationToken);

      return Ok(requests);
    }

    // GET: api/special-requests/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpecialRequestResponseDto>> GetRequestById(Guid id, CancellationToken cancellationToken)
    {
      var request = await _service.GetRequestById(id, cancellationToken);

      if (request is null)
      {
        return NotFound(new
        {
          message = $"Special request {id} was not found."
        });
      }

      return Ok(request);
    }

    // GET: api/special-requests/booking/{bookingId}
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SpecialRequestResponseDto>>> GetRequestByBookingId(Guid bookingId, CancellationToken cancellationToken)
    {
      var requests = await _service.GetRequestByBookingId(bookingId, cancellationToken);

      return Ok(requests);
    }

    // POST: api/special-requests
    [HttpPost]
    public async Task<ActionResult<SpecialRequestResponseDto>> Create(CreateSpecialRequestDto dto, CancellationToken cancellationToken)
    {
      var createdRequest = await _service.CreateRequest(dto, cancellationToken);

      // CreateAsync returns null when the BookingId does not exist in the SQL Booking database.
      if (createdRequest is null)
      {
        return NotFound(new
        {
          message = $"Booking {dto.BookingId} was not found."
        });
      }

      return CreatedAtAction(nameof(GetRequestById), new { id = createdRequest.SpecialRequestId }, createdRequest);
    }

    // PUT: api/special-requests/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSpecialRequestDto dto, CancellationToken cancellationToken)
    {
      try
      {
        var updated = await _service.UpdateRequest(id, dto, cancellationToken);

        if (!updated)
        {
          return NotFound(new
          {
            message = $"Special request {id} was not found."
          });
        }

        return NoContent();
      }
      catch (ArgumentException exception)
      {
        return BadRequest(new
        {
          message = exception.Message
        });
      }
    }

    // DELETE: api/special-requests/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
      var deleted = await _service.DeleteRequest(id, cancellationToken);

      if (!deleted)
      {
        return NotFound(new
        {
          message = $"Special request {id} was not found."
        });
      }

      return NoContent();
    }
  }
}
