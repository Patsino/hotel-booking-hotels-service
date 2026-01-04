using Application.Commands;
using Application.Dtos;
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
	[Route("api/hotels")]
	public sealed class HotelsController : ControllerBase
	{
		private readonly IHotelsRepository _hotelsRepo;
		private readonly IRoomsRepository _roomsRepo;
		private readonly IReservationsServiceClient _reservationsClient;
		private readonly ICurrentUserService _currentUser;
		private readonly IResourceAuthorizationService _authorizationService;
		private readonly ILogger<HotelsController> _logger;

		public HotelsController(
			IHotelsRepository hotelsRepo,
			IRoomsRepository roomsRepo,
			IReservationsServiceClient reservationsClient,
			ICurrentUserService currentUser,
			IResourceAuthorizationService authorizationService,
			ILogger<HotelsController> logger)
		{
			_hotelsRepo = hotelsRepo;
			_roomsRepo = roomsRepo;
			_reservationsClient = reservationsClient;
			_currentUser = currentUser;
			_authorizationService = authorizationService;
			_logger = logger;
		}

		/// <summary>
		/// Create a new hotel
		/// </summary>
		/// <param name="command">Hotel details</param>
		/// <returns>Created hotel ID</returns>
		/// <remarks>
		/// Creates a new hotel with Pending approval status. Requires HotelOwner or Admin role.
		/// 
		/// Sample request:
		/// 
		///     POST /api/hotels
		///     {
		///        "ownerId": 103,
		///        "name": "Grand Plaza Hotel",
		///        "country": "Latvia",
		///        "city": "Riga",
		///        "description": "Luxury 5-star hotel in the heart of Old Town",
		///        "district": "Centrs",
		///        "addressLine": "123 Brivibas Street",
		///        "petsAllowed": true,
		///        "isPetHotel": false,
		///        "cancelFreeDaysBefore": 7
		///     }
		/// 
		/// **Cancellation Policy:** cancelFreeDaysBefore defines free cancellation period (0-30 days)
		/// </remarks>
		/// <response code="201">Hotel created successfully, pending admin approval</response>
		/// <response code="400">Invalid input data</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not HotelOwner or Admin</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost]
		[SwaggerOperation(Summary = "Create hotel", Description = "Create new hotel with Pending status", OperationId = "CreateHotel", Tags = new[] { "Hotels" })]
		[SwaggerResponse(201, "Hotel created")]
		[SwaggerResponse(400, "Invalid request")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - HotelOwner or Admin role required")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> Create([FromBody] CreateHotelCommand command)
		{
			if (!_currentUser.IsAdmin && command.OwnerId != _currentUser.UserId)
			{
				return Forbid();
			}

			var hotel = new Hotel(command.OwnerId, command.Name, command.Country, command.City);
			hotel.Update(command.Name, command.Description, command.District, command.AddressLine,
				command.PetsAllowed, command.IsPetHotel, command.CancelFreeDaysBefore);

			await _hotelsRepo.AddAsync(hotel);
			await _hotelsRepo.SaveChangesAsync();

			_logger.LogInformation("Hotel created: {HotelId} by User {UserId}",
				hotel.Id, _currentUser.UserId);
			return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, new { id = hotel.Id });
		}

		/// <summary>
		/// Get hotel by ID
		/// </summary>
		/// <param name="id">Hotel ID</param>
		/// <returns>Hotel details</returns>
		/// <remarks>
		/// Returns hotel details. Only approved hotels are visible to unauthenticated users and non-owners.
		/// Hotel owners and admins can see their hotels in any status.
		/// 
		/// **Response includes:**
		/// - id, ownerId, name, description
		/// - country, city, district, addressLine
		/// - petsAllowed, isPetHotel, cancelFreeDaysBefore
		/// - approval (Pending, Approved, or Rejected)
		/// - submittedAt, reviewedAt timestamps
		/// </remarks>
		/// <response code="200">Hotel details retrieved</response>
		/// <response code="404">Hotel not found or not approved</response>
		[HttpGet("{id}")]
		[SwaggerOperation(Summary = "Get hotel by ID", Description = "Retrieve specific hotel details", OperationId = "GetHotelById", Tags = new[] { "Hotels" })]
		[SwaggerResponse(200, "Hotel details")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetById(int id)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			// Only show approved hotels to non-owners/non-admins
			if (!_currentUser.IsAuthenticated)
			{
				if (hotel.Approval != ApprovalStatus.Approved)
					return NotFound();
			}
			else if (!_currentUser.IsAdmin && hotel.OwnerId != _currentUser.UserId)
			{
				if (hotel.Approval != ApprovalStatus.Approved)
					return NotFound();
			}

			return Ok(new
			{
				hotel.Id,
				hotel.OwnerId,
				hotel.Name,
				hotel.Description,
				hotel.Country,
				hotel.City,
				hotel.District,
				hotel.AddressLine,
				hotel.PetsAllowed,
				hotel.IsPetHotel,
				hotel.CancelFreeDaysBefore,
				Approval = hotel.Approval.ToString(),
				hotel.SubmittedAt,
				hotel.ReviewedAt
			});
		}

		/// <summary>
		/// Get current user's hotels
		/// </summary>
		/// <returns>List of hotels owned by current user</returns>
		/// <remarks>
		/// Returns all hotels created by the authenticated user (HotelOwner or Admin).
		/// 
		/// **No request parameters required.**
		/// 
		/// **Response includes:** id, name, country, city, approval status, submittedAt
		/// </remarks>
		/// <response code="200">List of user's hotels</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not HotelOwner or Admin</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpGet("mine")]
		[SwaggerOperation(Summary = "Get my hotels", Description = "Retrieve hotels owned by current user", OperationId = "GetMyHotels", Tags = new[] { "Hotels" })]
		[SwaggerResponse(200, "List of hotels")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - HotelOwner or Admin role required")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> GetMine()
		{
			if (!_currentUser.UserId.HasValue)
				return Unauthorized();

			var hotels = await _hotelsRepo.GetByOwnerIdAsync(_currentUser.UserId.Value);
			return Ok(hotels.Select(h => new
			{
				h.Id,
				h.Name,
				h.Country,
				h.City,
				Approval = h.Approval.ToString(),
				h.SubmittedAt
			}));
		}

		/// <summary>
		/// Get reservations for a specific hotel
		/// </summary>
		/// <param name="id">Hotel ID</param>
		/// <returns>List of reservations for all rooms in the hotel</returns>
		/// <remarks>
		/// Returns all reservations for a hotel. Only the hotel owner or admin can access.
		/// 
		/// **Response includes:**
		/// - id, userId, roomId
		/// - startDate, endDate (YYYY-MM-DD format)
		/// - guestsCount, guestsNames
		/// - status (Pending, Held, Confirmed, Canceled)
		/// - cancellationStatus (None, Requested, AutoCanceled, AdminApproved, AdminRejected)
		/// - cancellationReason, cancellationRequestedAt
		/// - createdAt timestamp
		/// - roomNumber (from hotel's room data)
		/// </remarks>
		/// <response code="200">List of reservations</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		/// <response code="404">Hotel not found</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpGet("{id}/reservations")]
		[SwaggerOperation(Summary = "Get hotel reservations", Description = "Retrieve all reservations for a hotel", OperationId = "GetHotelReservations", Tags = new[] { "Hotels" })]
		[SwaggerResponse(200, "List of reservations")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden - must be hotel owner or admin")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetHotelReservations(int id)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			// Check authorization - only owner or admin can see reservations
			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			// Get all rooms for this hotel
			var rooms = await _roomsRepo.GetByHotelIdAsync(id);
			if (rooms.Count == 0)
			{
				return Ok(new List<object>());
			}

			var roomIds = rooms.Select(r => r.Id).ToList();
			var roomNumberMap = rooms.ToDictionary(r => r.Id, r => r.RoomNumber);

			// Get reservations from Reservations Service
			var reservations = await _reservationsClient.GetReservationsByRoomIdsAsync(roomIds);

			// Enrich with room number
			var result = reservations.Select(r => new
			{
				r.Id,
				r.UserId,
				r.RoomId,
				RoomNumber = roomNumberMap.TryGetValue(r.RoomId, out var num) ? num : "Unknown",
				r.StartDate,
				r.EndDate,
				r.GuestsCount,
				r.GuestsNames,
				r.Status,
				r.CancellationStatus,
				r.CancellationReason,
				r.CancellationRequestedAt,
				r.CreatedAt
			});

			_logger.LogInformation("Hotel {HotelId} reservations retrieved by User {UserId}: {Count} reservations",
				id, _currentUser.UserId, reservations.Count);

			return Ok(result);
		}

		/// <summary>
		/// Update hotel details
		/// </summary>
		/// <param name="id">Hotel ID to update</param>
		/// <param name="command">Updated hotel details</param>
		/// <returns>No content on success</returns>
		/// <remarks>
		/// Updates hotel information. Only hotel owner or admin can update.
		/// 
		/// Sample request:
		/// 
		///     PATCH /api/hotels/15
		///     {
		///        "name": "Updated Hotel Name",
		///        "description": "New description",
		///        "district": "Centrs",
		///        "addressLine": "456 New Street",
		///        "petsAllowed": true,
		///        "isPetHotel": false,
		///        "cancelFreeDaysBefore": 10
		///     }
		/// </remarks>
		/// <response code="204">Hotel updated successfully</response>
		/// <response code="401">User not authenticated</response>
		/// <response code="403">User is not hotel owner or admin</response>
		/// <response code="404">Hotel not found</response>
		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPatch("{id}")]
		[SwaggerOperation(Summary = "Update hotel", Description = "Update hotel details", OperationId = "UpdateHotel", Tags = new[] { "Hotels" })]
		[SwaggerResponse(204, "Hotel updated")]
		[SwaggerResponse(401, "Unauthorized")]
		[SwaggerResponse(403, "Forbidden")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Update(int id, [FromBody] UpdateHotelCommand command)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			// Check authorization
			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			hotel.Update(command.Name, command.Description, command.District, command.AddressLine,
				command.PetsAllowed, command.IsPetHotel, command.CancelFreeDaysBefore);

			await _hotelsRepo.SaveChangesAsync();
			_logger.LogInformation("Hotel updated: {HotelId} by User {UserId}",
				id, _currentUser.UserId);
			return NoContent();
		}

		/// <summary>
		/// Search for available hotels
		/// </summary>
		/// <param name="query">Search criteria including dates, location, guests, pets, price range</param>
		/// <returns>List of hotels with available rooms matching criteria</returns>
		/// <remarks>
		/// Searches for approved hotels with available rooms.
		/// 
		/// **Query Parameters:**
		/// - country, city, district: Location filters
		/// - startDate, endDate: Check-in/out dates (YYYY-MM-DD format)
		/// - guestsCount: Number of guests (1-20)
		/// - withPets: Pet-friendly filter (true/false)
		/// - isPetHotelOnly: Pet hotels only (true/false)
		/// - accommodation: Room type (HotelRoom, Apartment, Villa, Bungalow, etc.)
		/// - minPrice, maxPrice: Price range per night in EUR
		/// 
		/// **Example:**
		///     GET /api/hotels/search?country=Latvia&amp;city=Riga&amp;startDate=2024-12-20&amp;endDate=2024-12-25&amp;guestsCount=2&amp;minPrice=50&amp;maxPrice=150
		/// </remarks>
		/// <response code="200">List of matching hotels with rooms and prices</response>
		/// <response code="400">Invalid search parameters</response>
		/// <response code="500">Search failed</response>
		[HttpGet("search")]
		[SwaggerOperation(Summary = "Search hotels", Description = "Find available hotels matching criteria", OperationId = "SearchHotels", Tags = new[] { "Hotels" })]
		[SwaggerResponse(200, "Search results")]
		[SwaggerResponse(400, "Invalid parameters")]
		[SwaggerResponse(500, "Search failed")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> Search([FromQuery] SearchHotelsQuery query)
		{
			try
			{
				// Step 1: Get all hotels with rooms matching filters (from hotels schema only)
				var hotelsWithRooms = await _hotelsRepo.SearchHotelsWithRoomsAsync(query);

				// Step 2: If dates are provided, check room availability via Reservations service
				if (query.StartDate.HasValue && query.EndDate.HasValue && hotelsWithRooms.Count > 0)
				{
					// Collect all room IDs from search results
					var allRoomIds = hotelsWithRooms
						.SelectMany(h => h.Rooms)
						.Select(r => r.RoomId)
						.ToList();

					_logger.LogDebug(
						"Checking availability for {RoomCount} rooms from {StartDate} to {EndDate}",
						allRoomIds.Count, query.StartDate, query.EndDate);

					// Get unavailable room IDs from Reservations service (batch call)
					var unavailableRoomIds = await _reservationsClient.GetUnavailableRoomIdsAsync(
						allRoomIds,
						query.StartDate.Value,
						query.EndDate.Value);

					// Filter out unavailable rooms from results
					if (unavailableRoomIds.Count > 0)
					{
						var unavailableSet = unavailableRoomIds.ToHashSet();

						hotelsWithRooms = hotelsWithRooms
							.Select(h => new HotelSearchResultDto(
								h.HotelId,
								h.HotelName,
								h.Description,
								h.Country,
								h.City,
								h.District,
								h.AddressLine,
								h.PetsAllowed,
								h.IsPetHotel,
								h.CancelFreeDaysBefore,
								h.Rooms.Where(r => !unavailableSet.Contains(r.RoomId)).ToList()
							))
							.Where(h => h.Rooms.Count > 0) // Only keep hotels with available rooms
							.ToList();

						_logger.LogDebug(
							"After availability filtering: {HotelCount} hotels with available rooms",
							hotelsWithRooms.Count);
					}
				}

				return Ok(hotelsWithRooms);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during hotel search");
				return StatusCode(500, new { error = "Search failed" });
			}
		}



		/// <summary>
		/// Get all rooms for a specific hotel
		/// </summary>
		/// <param name="hotelId">Hotel ID</param>
		/// <returns>List of rooms in the hotel</returns>
		/// <remarks>
		/// Returns all rooms for a hotel. Only visible rooms are shown to non-owners/non-admins.
		/// Hotel owners and admins can see all rooms including hidden ones.
		/// 
		/// **Response includes:**
		/// - id, hotelId, roomNumber, description
		/// - capacity, bedrooms, pricePerNight
		/// - visible, petsAllowed
		/// - accommodation (HotelRoom, Apartment, Villa, etc.)
		/// - createdAt timestamp
		/// </remarks>
		/// <response code="200">List of rooms</response>
		/// <response code="404">Hotel not found or not approved</response>
		[HttpGet("{hotelId}/rooms")]
		[SwaggerOperation(Summary = "Get hotel rooms", Description = "Retrieve all rooms for a hotel", OperationId = "GetHotelRooms", Tags = new[] { "Hotels" })]
		[SwaggerResponse(200, "List of rooms")]
		[SwaggerResponse(404, "Hotel not found")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetHotelRooms(int hotelId)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(hotelId);
			if (hotel == null)
				return NotFound();

			// Determine if user can see hidden rooms and hotels
			var isOwnerOrAdmin = _currentUser.IsAdmin || hotel.OwnerId == _currentUser.UserId;

            if (hotel.Approval != ApprovalStatus.Approved && !isOwnerOrAdmin)
                return NotFound();

            var rooms = await _roomsRepo.GetByHotelIdAsync(hotelId, includeHidden: isOwnerOrAdmin);

			return Ok(rooms.Select(r => new
			{
				r.Id,
				r.HotelId,
				r.RoomNumber,
				r.Description,
				r.Capacity,
				r.Bedrooms,
				r.PricePerNight,
				r.Visible,
				r.PetsAllowed,
				Accommodation = r.Accommodation.ToString(),
				r.CreatedAt
			}));
		}
	}
}
