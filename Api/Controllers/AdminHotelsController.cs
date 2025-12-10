using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers
{
	[Authorize(Policy = "AdminOnly")]
	[ApiController]
	[Route("api/admin/hotels")]
	public sealed class AdminHotelsController : ControllerBase
	{
		private readonly IHotelsRepository _repository;
		private readonly ILogger<AdminHotelsController> _logger;

		public AdminHotelsController(
			IHotelsRepository repository,
			ILogger<AdminHotelsController> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		/// <summary>
		/// Get all pending hotel approval requests (Admin only)
		/// </summary>
		/// <returns>List of hotels awaiting approval</returns>
		/// <remarks>
		/// Returns all hotels with Pending approval status.
		/// 
		/// **No request parameters required.**
		/// 
		/// **Response includes:** id, ownerId, name, country, city, district, description, submittedAt, approval
		/// </remarks>
		/// <response code="200">List of pending hotels</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not Admin</response>
		[HttpGet("pending")]
		[SwaggerOperation(Summary = "Get pending hotels", Description = "Retrieve hotels awaiting approval", OperationId = "GetPendingHotels", Tags = new[] { "Hotels - Admin" })]
		[SwaggerResponse(200, "List of pending hotels")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - Admin role required")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> GetPending()
		{
			var hotels = await _repository.GetPendingAsync();

			_logger.LogInformation("Admin retrieved {Count} pending hotels", hotels.Count);

			return Ok(hotels.Select(h => new
			{
				h.Id,
				h.OwnerId,
				h.Name,
				h.Country,
				h.City,
				h.District,
				h.Description,
				h.SubmittedAt,
				Approval = h.Approval.ToString()
			}));
		}

		/// <summary>
		/// Approve a pending hotel (Admin only)
		/// </summary>
		/// <param name="id">Hotel ID to approve</param>
		/// <returns>Approval confirmation</returns>
		/// <remarks>
		/// Approves a hotel, making it publicly visible in search results.
		/// 
		/// **No request body required.**
		/// 
		/// **Response includes:** message, hotelId, approvedAt timestamp
		/// </remarks>
		/// <response code="200">Hotel approved successfully</response>
		/// <response code="400">Hotel already approved</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not Admin</response>
		/// <response code="404">Hotel not found</response>
		[HttpPost("{id}/approve")]
		[SwaggerOperation(Summary = "Approve hotel", Description = "Approve pending hotel", OperationId = "ApproveHotel", Tags = new[] { "Hotels - Admin" })]
		[SwaggerResponse(200, "Hotel approved")]
		[SwaggerResponse(400, "Already approved")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - Admin role required")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Approve(int id)
		{
			var hotel = await _repository.GetByIdAsync(id);
			if (hotel == null)
			{
				_logger.LogWarning("Admin attempted to approve non-existent hotel: {HotelId}", id);
				return NotFound(new { error = "Hotel not found" });
			}

			if (hotel.Approval == ApprovalStatus.Approved)
			{
				return BadRequest(new { error = "Hotel is already approved" });
			}

			hotel.Approve();
			await _repository.SaveChangesAsync();

			_logger.LogInformation("Hotel approved: {HotelId} by Admin", id);

			return Ok(new
			{
				message = "Hotel approved successfully",
				hotelId = id,
				approvedAt = hotel.ReviewedAt
			});
		}

		/// <summary>
		/// Reject a pending hotel (Admin only)
		/// </summary>
		/// <param name="id">Hotel ID to reject</param>
		/// <returns>Rejection confirmation</returns>
		/// <remarks>
		/// Rejects a hotel application, preventing it from appearing in search results.
		/// 
		/// **No request body required.**
		/// 
		/// **Response includes:** message, hotelId, rejectedAt timestamp
		/// </remarks>
		/// <response code="200">Hotel rejected successfully</response>
		/// <response code="400">Hotel already rejected</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not Admin</response>
		/// <response code="404">Hotel not found</response>
		[HttpPost("{id}/reject")]
		[SwaggerOperation(Summary = "Reject hotel", Description = "Reject pending hotel", OperationId = "RejectHotel", Tags = new[] { "Hotels - Admin" })]
		[SwaggerResponse(200, "Hotel rejected")]
		[SwaggerResponse(400, "Already rejected")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - Admin role required")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Reject(int id)
		{
			var hotel = await _repository.GetByIdAsync(id);
			if (hotel == null)
			{
				_logger.LogWarning("Admin attempted to reject non-existent hotel: {HotelId}", id);
				return NotFound(new { error = "Hotel not found" });
			}

			if (hotel.Approval == ApprovalStatus.Rejected)
			{
				return BadRequest(new { error = "Hotel is already rejected" });
			}

			hotel.Reject();
			await _repository.SaveChangesAsync();

			_logger.LogInformation("Hotel rejected: {HotelId} by Admin", id);

			return Ok(new
			{
				message = "Hotel rejected",
				hotelId = id,
				rejectedAt = hotel.ReviewedAt
			});
		}

		/// <summary>
		/// Get all hotels with optional status filter (Admin only)
		/// </summary>
		/// <param name="status">Filter by approval status (Pending, Approved, or Rejected)</param>
		/// <returns>List of hotels</returns>
		/// <remarks>
		/// Returns all hotels in the system with optional status filtering.
		/// 
		/// **Query Examples:**
		/// - GET /api/admin/hotels/all
		/// - GET /api/admin/hotels/all?status=Approved
		/// - GET /api/admin/hotels/all?status=Rejected
		/// 
		/// **Response includes:** id, ownerId, name, country, city, approval, submittedAt
		/// </remarks>
		/// <response code="200">List of hotels</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not Admin</response>
		[HttpGet("all")]
		[SwaggerOperation(Summary = "Get all hotels", Description = "Retrieve all hotels with optional status filter", OperationId = "GetAllHotelsAdmin", Tags = new[] { "Hotels - Admin" })]
		[SwaggerResponse(200, "List of hotels")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - Admin role required")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> GetAll(
			[FromQuery] string? status = null)
		{
			var allHotels = await _repository.GetAllAsync();

			if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var statusEnum))
			{
				allHotels = allHotels.Where(h => h.Approval == statusEnum).ToList();
			}

			return Ok(allHotels.Select(h => new
			{
				h.Id,
				h.OwnerId,
				h.Name,
				h.Country,
				h.City,
				Approval = h.Approval.ToString(),
				h.SubmittedAt,
				h.ReviewedAt
			}));
		}
	}
}
