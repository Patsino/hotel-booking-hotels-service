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
	[Route("api/hotels")]
	public sealed class HotelsController : ControllerBase
	{
		private readonly IHotelsRepository _hotelsRepo;
		private readonly IRoomsRepository _roomsRepo;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICurrentUserService _currentUser;
		private readonly IResourceAuthorizationService _authorizationService;
		private readonly ILogger<HotelsController> _logger;

		public HotelsController(
			IHotelsRepository hotelsRepo,
			IRoomsRepository roomsRepo,
			IHttpClientFactory httpClientFactory,
			ICurrentUserService currentUser,
			IResourceAuthorizationService authorizationService,
			ILogger<HotelsController> logger)
		{
			_hotelsRepo = hotelsRepo;
			_roomsRepo = roomsRepo;
			_httpClientFactory = httpClientFactory;
			_currentUser = currentUser;
			_authorizationService = authorizationService;
			_logger = logger;
		}

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateHotelCommand command)
		{
			// Ensure user can only create hotels for themselves (unless admin)
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

		[HttpGet("{id}")]
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

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpGet("mine")]
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

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPatch("{id}")]
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

		[Authorize(Policy = "HotelOwnerOrAdmin")]
		[HttpPost("{id}/submit")]
		public async Task<IActionResult> Submit(int id)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(id);
			if (hotel == null)
				return NotFound();

			// Check authorization
			_authorizationService.EnsureCanModifyResource(hotel.OwnerId);

			hotel.Submit();
			await _hotelsRepo.SaveChangesAsync();
			_logger.LogInformation("Hotel submitted for review: {HotelId} by User {UserId}",
				id, _currentUser.UserId);
			return NoContent();
		}

		[HttpGet("search")]
		public async Task<IActionResult> Search([FromQuery] SearchHotelsQuery query)
		{
			try
			{
				var results = await _hotelsRepo.SearchAvailableHotelsAsync(query);
				return Ok(results);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during hotel search");
				return StatusCode(500, new { error = "Search failed" });
			}
		}

		//[HttpGet("search")]
		//public async Task<IActionResult> Search([FromQuery] SearchHotelsQuery query)
		//{
		//	// Public endpoint - anyone can search
		//	var hotels = await _hotelsRepo.SearchAsync(
		//		query.Country, query.City, query.District,
		//		query.WithPets, query.IsPetHotelOnly);

		//	if (!hotels.Any())
		//		return Ok(new List<object>());

		//	var hotelIds = hotels.Select(h => h.Id).ToList();

		//	var rooms = await _roomsRepo.SearchRoomsAsync(
		//		hotelIds, query.GuestsCount, query.Accommodation,
		//		query.MinPrice, query.MaxPrice, query.WithPets);

		//	// If date filtering requested, check availability
		//	if (query.StartDate.HasValue && query.EndDate.HasValue)
		//	{
		//		var reservationsClient = _httpClientFactory.CreateClient("ReservationsService");
		//		var availableRoomIds = new List<int>();

		//		foreach (var room in rooms)
		//		{
		//			var response = await reservationsClient.GetAsync(
		//				$"/internal/reservations/room/{room.Id}/busy-ranges?start={query.StartDate:yyyy-MM-dd}&end={query.EndDate:yyyy-MM-dd}");

		//			if (response.IsSuccessStatusCode)
		//			{
		//				var busyRanges = await response.Content.ReadFromJsonAsync<List<object>>();
		//				if (busyRanges?.Count == 0)
		//				{
		//					availableRoomIds.Add(room.Id);
		//				}
		//			}
		//		}

		//		rooms = rooms.Where(r => availableRoomIds.Contains(r.Id)).ToList();
		//	}

		//	var results = hotels
		//		.Select(h => new
		//		{
		//			h.Id,
		//			h.Name,
		//			h.Description,
		//			h.Country,
		//			h.City,
		//			h.District,
		//			h.AddressLine,
		//			h.PetsAllowed,
		//			h.IsPetHotel,
		//			h.CancelFreeDaysBefore,
		//			Rooms = rooms.Where(r => r.HotelId == h.Id).Select(r => new
		//			{
		//				r.Id,
		//				r.RoomNumber,
		//				r.Description,
		//				r.Capacity,
		//				r.Bedrooms,
		//				r.PricePerNight,
		//				r.PetsAllowed,
		//				Accommodation = r.Accommodation.ToString()
		//			})
		//		})
		//		.Where(h => h.Rooms.Any())
		//		.ToList();

		//	return Ok(results);
		//}

		//[Authorize(Policy = "HotelOwnerOrAdmin")]
		//[HttpGet("{hotelId}/rooms")]
		//public async Task<IActionResult> GetRooms(int hotelId, [FromQuery] bool includeHidden = false)
		//{
		//	var hotel = await _hotelsRepo.GetByIdAsync(hotelId);
		//	if (hotel == null)
		//		return NotFound();

		//	// Check authorization - only owner or admin can see all rooms
		//	var canSeeHidden = _currentUser.IsAdmin || hotel.OwnerId == _currentUser.UserId;

		//	var rooms = await _roomsRepo.GetByHotelIdAsync(hotelId, canSeeHidden && includeHidden);
		//	return Ok(rooms.Select(r => new
		//	{
		//		r.Id,
		//		r.HotelId,
		//		r.RoomNumber,
		//		r.Description,
		//		r.Capacity,
		//		r.Bedrooms,
		//		r.PricePerNight,
		//		r.Visible,
		//		r.PetsAllowed,
		//		Accommodation = r.Accommodation.ToString(),
		//		r.CreatedAt
		//	}));
		//}

		[HttpGet("{hotelId}/rooms")]
		public async Task<IActionResult> GetHotelRooms(int hotelId)
		{
			var hotel = await _hotelsRepo.GetByIdAsync(hotelId);
			if (hotel == null)
				return NotFound();

			// Only show approved hotels to public
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

			// Determine if user can see hidden rooms
			var canSeeHidden = _currentUser.IsAdmin || hotel.OwnerId == _currentUser.UserId;

			var rooms = await _roomsRepo.GetByHotelIdAsync(hotelId, includeHidden: canSeeHidden);

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
