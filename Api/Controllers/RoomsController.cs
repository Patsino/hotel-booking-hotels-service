using Application.Commands;
using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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

		/// <summary>
		/// Create a new room for a hotel
		/// </summary>
		/// <param name="command">Room details</param>
		/// <returns>Created room ID</returns>
		/// <remarks>
		/// Adds a new room to a hotel. Only hotel owner or admin can create rooms.
		/// 
		/// Sample request:
		/// 
		///     POST /api/rooms
		///     {
		///        "hotelId": 15,
		///        "capacity": 2,
		///        "bedrooms": 1,
		///        "pricePerNight": 89.00,
		///        "roomNumber": "203",
		///        "description": "Cozy room with city view and balcony",
		///        "petsAllowed": true,
		///        "accommodation": "HotelRoom"
		///     }
		/// 
		/// **Validation:**
		/// - capacity: 1-20 guests
		/// - bedrooms: 1-10
		/// - pricePerNight: minimum €0.01
		/// - accommodation: HotelRoom, Apartment, House, Cabin, Capsule
		/// </remarks>
		/// <response code="201">Room created successfully</response>
		/// <response code="400">Invalid input or hotel not found</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost]
		[SwaggerOperation(Summary = "Create room", Description = "Add new room to hotel", OperationId = "CreateRoom", Tags = new[] { "Rooms" })]
		[SwaggerResponse(201, "Room created")]
		[SwaggerResponse(400, "Invalid request")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
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

		/// <summary>
		/// Get room by ID
		/// </summary>
		/// <param name="id">Room ID</param>
		/// <returns>Room details</returns>
		/// <remarks>
		/// Returns room details. Hidden rooms are only visible to hotel owner and admin.
		/// 
		/// **Response includes:**
		/// - id, hotelId, roomNumber, description
		/// - capacity, bedrooms, pricePerNight
		/// - visible, petsAllowed
		/// - accommodation (HotelRoom, Apartment, House, Cabin, Capsule)
		/// - createdAt timestamp
		/// </remarks>
		/// <response code="200">Room details retrieved</response>
		/// <response code="404">Room not found or hidden</response>
		[HttpGet("{id}")]
		[SwaggerOperation(Summary = "Get room by ID", Description = "Retrieve specific room details", OperationId = "GetRoomById", Tags = new[] { "Rooms" })]
		[SwaggerResponse(200, "Room details")]
		[SwaggerResponse(404, "Room not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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

		/// <summary>
		/// Update room details
		/// </summary>
		/// <param name="id">Room ID to update</param>
		/// <param name="command">Updated room details</param>
		/// <returns>No content on success</returns>
		/// <remarks>
		/// Updates room information. Only hotel owner or admin can update.
		/// 
		/// Sample request:
		/// 
		///     PATCH /api/rooms/55
		///     {
		///        "roomNumber": "203A",
		///        "description": "Updated description",
		///        "capacity": 3,
		///        "bedrooms": 1,
		///        "pricePerNight": 95.00,
		///        "petsAllowed": false,
		///        "accommodation": "Suite"
		///     }
		/// </remarks>
		/// <response code="204">Room updated successfully</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		/// <response code="404">Room not found</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPatch("{id}")]
		[SwaggerOperation(Summary = "Update room", Description = "Update room details", OperationId = "UpdateRoom", Tags = new[] { "Rooms" })]
		[SwaggerResponse(204, "Room updated")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden")]
		[SwaggerResponse(404, "Room not found")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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

		/// <summary>
		/// Hide room from public search
		/// </summary>
		/// <param name="id">Room ID to hide</param>
		/// <returns>No content on success</returns>
		/// <remarks>
		/// Makes room invisible in public searches. Hidden rooms cannot be booked.
		/// 
		/// **No request body required.**
		/// </remarks>
		/// <response code="204">Room hidden successfully</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		/// <response code="404">Room not found</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost("{id}/hide")]
		[SwaggerOperation(Summary = "Hide room", Description = "Make room invisible in searches", OperationId = "HideRoom", Tags = new[] { "Rooms" })]
		[SwaggerResponse(204, "Room hidden")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden")]
		[SwaggerResponse(404, "Room not found")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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

		/// <summary>
		/// Make hidden room visible in public search
		/// </summary>
		/// <param name="id">Room ID to show</param>
		/// <returns>No content on success</returns>
		/// <remarks>
		/// Makes a previously hidden room visible and available for booking.
		/// 
		/// **No request body required.**
		/// </remarks>
		/// <response code="204">Room shown successfully</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		/// <response code="404">Room not found</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost("{id}/show")]
		[SwaggerOperation(Summary = "Show room", Description = "Make room visible in searches", OperationId = "ShowRoom", Tags = new[] { "Rooms" })]
		[SwaggerResponse(204, "Room shown")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden")]
		[SwaggerResponse(404, "Room not found")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
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
