using Application.Services;
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

		public AdminHotelsController(IHotelsRepository repository, ILogger<AdminHotelsController> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		[HttpGet("pending")]
		public async Task<IActionResult> GetPending()
		{
			var hotels = await _repository.GetPendingAsync();
			return Ok(hotels.Select(h => new
			{
				h.Id,
				h.OwnerId,
				h.Name,
				h.Country,
				h.City,
				h.SubmittedAt
			}));
		}

		[HttpPost("{id}/approve")]
		public async Task<IActionResult> Approve(int id)
		{
			var hotel = await _repository.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			hotel.Approve();
			await _repository.SaveChangesAsync();
			_logger.LogInformation("Hotel approved: {HotelId} by Admin", id);
			return NoContent();
		}

		[HttpPost("{id}/reject")]
		public async Task<IActionResult> Reject(int id)
		{
			var hotel = await _repository.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			hotel.Reject();
			await _repository.SaveChangesAsync();
			_logger.LogInformation("Hotel rejected: {HotelId} by Admin", id);
			return NoContent();
		}
	}
}
