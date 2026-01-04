using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace Api.Controllers
{
	[ExcludeFromCodeCoverage]
	[Authorize(Policy = "ServiceToService")]
	[ApiController]
	[Route("internal/hotels")]
	public sealed class InternalHotelsController : ControllerBase
	{
		private readonly IRoomsRepository _roomsRepo;
		private readonly ILogger<InternalHotelsController> _logger;

		public InternalHotelsController(
			IRoomsRepository roomsRepo,
			ILogger<InternalHotelsController> logger)
		{
			_roomsRepo = roomsRepo;
			_logger = logger;
		}

		/// <summary>
		/// Deactivate all hotels owned by a user.
		/// Used by Users Service when user account is deleted (GDPR).
		/// </summary>
		[HttpPost("owners/{ownerId}/deactivate")]
		public async Task<IActionResult> DeactivateOwnerHotels(int ownerId)
		{
			await _roomsRepo.HideRoomsByOwnerAsync(ownerId);
			_logger.LogInformation("Internal API: Owner {OwnerId} hotels deactivated", ownerId);
			return NoContent();
		}
	}
}
