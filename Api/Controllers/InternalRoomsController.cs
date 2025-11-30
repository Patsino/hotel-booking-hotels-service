using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
	[Authorize(Policy = "ServiceToService")]
	[ApiController]
	[Route("internal/rooms")]
	public sealed class InternalRoomsController : ControllerBase
	{
		private readonly IRoomsRepository _roomsRepo;
		private readonly IHotelsRepository _hotelsRepo;
		private readonly ILogger<InternalRoomsController> _logger;

		public InternalRoomsController(
			IRoomsRepository roomsRepo,
			IHotelsRepository hotelsRepo,
			ILogger<InternalRoomsController> logger)
		{
			_roomsRepo = roomsRepo;
			_hotelsRepo = hotelsRepo;
			_logger = logger;
		}

		[HttpGet("{roomId}/details")]
		public async Task<IActionResult> GetRoomDetails(int roomId)
		{
			var room = await _roomsRepo.GetByIdAsync(roomId);
			if (room == null || !room.Visible)
			{
				return NotFound();
			}

			var hotel = await _hotelsRepo.GetByIdAsync(room.HotelId);
			if (hotel == null)
			{
				return NotFound();
			}

			_logger.LogInformation("Internal API: Room {RoomId} details accessed", roomId);

			return Ok(new
			{
				room.Id,
				room.HotelId,
				room.Capacity,
				room.PetsAllowed,
				Accommodation = room.Accommodation.ToString(),
				hotel.CancelFreeDaysBefore,
				HotelApproval = hotel.Approval.ToString(),
				room.Visible
			});
		}
	}
}
