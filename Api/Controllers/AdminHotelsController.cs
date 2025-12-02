using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

		[HttpGet("pending")]
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

		[HttpPost("{id}/approve")]
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

		[HttpPost("{id}/reject")]
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

		[HttpGet("all")]
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
