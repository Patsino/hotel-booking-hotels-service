using Application.Commands;
using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
	[ApiController]
	[Route("api/rooms")]
	public sealed class RoomsController : ControllerBase
	{
		private readonly IRoomsRepository _roomsRepo;
		private readonly IHotelsRepository _hotelsRepo;
		private readonly IResourceAuthorizationService _authorizationService;
		private readonly ICurrentUserService _currentUser;
		private readonly ILogger<RoomsController> _logger;

		public RoomsController(
			IRoomsRepository roomsRepo,
			IHotelsRepository hotelsRepo,
			IResourceAuthorizationService authorizationService,
			ICurrentUserService currentUser,
			ILogger<RoomsController> logger)
		{
			_roomsRepo = roomsRepo;
			_hotelsRepo = hotelsRepo;
			_authorizationService = authorizationService;
			_currentUser = currentUser;
			_logger = logger;
		}

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateRoomCommand command)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(command.HotelId);
			if (hotel == null)
				return BadRequest(new { error = "Hotel not found" });

			// Check authorization - only hotel owner or admin
			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			var accommodation = Enum.Parse<AccommodationType>(command.Accommodation, true);
			var room = new Room(command.HotelId, command.Capacity, command.Bedrooms, command.PricePerNight);
			room.Update(command.RoomNumber, command.Description, command.Capacity,
				command.Bedrooms, command.PricePerNight, command.PetsAllowed, accommodation);

			await _roomsRepo.AddAsync(room);
			await _roomsRepo.SaveChangesAsync();

			_logger.LogInformation("Room created: {RoomId} by User {UserId}",
				room.Id, _currentUser.UserId);
			return CreatedAtAction(nameof(GetById), new { id = room.Id }, new { id = room.Id });
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var room = await _roomsRepo.GetByIdAsync(id);
			if (room == null)
				return NotFound();

			var hotel = await _hotelsRepo.GetByIdAsync(room.HotelId);
			if (hotel == null)
				return NotFound();

			if (!room.Visible)
			{
				var canSeeHidden = _currentUser.IsAdmin || hotel.OwnerId == _currentUser.UserId;

				if (!canSeeHidden)
				{
					return NotFound();
				}
			}

			return Ok(new
			{
				room.Id,
				room.HotelId,
				room.RoomNumber,
				room.Description,
				room.Capacity,
				room.Bedrooms,
				room.PricePerNight,
				room.Visible,
				room.PetsAllowed,
				Accommodation = room.Accommodation.ToString(),
				room.CreatedAt
			});
		}

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPatch("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomCommand command)
		{
			var room = await _roomsRepo.GetByIdAsync(id);
			if (room == null)
				return NotFound();

			var hotel = await _hotelsRepo.GetByIdAsync(room.HotelId);
			if (hotel == null)
				return NotFound();

			// Check authorization
			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			var accommodation = Enum.Parse<AccommodationType>(command.Accommodation, true);
			room.Update(command.RoomNumber, command.Description, command.Capacity,
				command.Bedrooms, command.PricePerNight, command.PetsAllowed, accommodation);

			await _roomsRepo.SaveChangesAsync();
			_logger.LogInformation("Room updated: {RoomId} by User {UserId}",
				id, _currentUser.UserId);
			return NoContent();
		}

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost("{id}/hide")]
		public async Task<IActionResult> Hide(int id)
		{
			var room = await _roomsRepo.GetByIdAsync(id);
			if (room == null)
				return NotFound();

			var hotel = await _hotelsRepo.GetByIdAsync(room.HotelId);
			if (hotel == null)
				return NotFound();

			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			room.Hide();
			await _roomsRepo.SaveChangesAsync();
			_logger.LogInformation("Room hidden: {RoomId} by User {UserId}",
				id, _currentUser.UserId);
			return NoContent();
		}

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost("{id}/show")]
		public async Task<IActionResult> Show(int id)
		{
			var room = await _roomsRepo.GetByIdAsync(id);
			if (room == null)
				return NotFound();

			var hotel = await _hotelsRepo.GetByIdAsync(room.HotelId);
			if (hotel == null)
				return NotFound();

			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			room.Show();
			await _roomsRepo.SaveChangesAsync();
			_logger.LogInformation("Room shown: {RoomId} by User {UserId}",
				id, _currentUser.UserId);
			return NoContent();
		}
	}
}
