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



		[HttpGet("{hotelId}/rooms")]
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
