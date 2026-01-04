using Application.Commands;
using Application.Dtos;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
	public sealed class HotelsRepository : IHotelsRepository
	{
		private readonly HotelsDbContext _context;

		public HotelsRepository(HotelsDbContext context) => _context = context;

		public async Task<Hotel?> GetByIdAsync(int id, CancellationToken ct = default)
			=> await _context.Hotels.FindAsync(new object[] { id }, ct);

		public async Task<List<Hotel>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default)
			=> await _context.Hotels.Where(h => h.OwnerId == ownerId).ToListAsync(ct);

		public async Task<List<Hotel>> GetPendingAsync(CancellationToken ct = default)
			=> await _context.Hotels.Where(h => h.Approval == ApprovalStatus.Pending).ToListAsync(ct);

		public async Task<List<Hotel>> GetAllAsync(CancellationToken ct = default)
			=> await _context.Hotels.OrderByDescending(h => h.SubmittedAt).ToListAsync(ct);

		public async Task AddAsync(Hotel hotel, CancellationToken ct = default)
			=> await _context.Hotels.AddAsync(hotel, ct);

		public async Task SaveChangesAsync(CancellationToken ct = default)
			=> await _context.SaveChangesAsync(ct);

		public async Task<List<HotelSearchResultDto>> SearchHotelsWithRoomsAsync(
			SearchHotelsQuery query,
			CancellationToken ct = default)
		{
			var hotelsQuery = _context.Hotels
				.Where(h => h.Approval == ApprovalStatus.Approved)
				.AsQueryable();

			if (!string.IsNullOrEmpty(query.Country))
				hotelsQuery = hotelsQuery.Where(h => h.Country == query.Country);

			if (!string.IsNullOrEmpty(query.City))
				hotelsQuery = hotelsQuery.Where(h => h.City == query.City);

			if (!string.IsNullOrEmpty(query.District))
				hotelsQuery = hotelsQuery.Where(h => h.District == query.District);

			if (query.IsPetHotelOnly == true)
				hotelsQuery = hotelsQuery.Where(h => h.IsPetHotel);

			var hotels = await hotelsQuery
				.Include(h => h.Rooms.Where(r => r.Visible))
				.ToListAsync(ct);

			var results = new List<HotelSearchResultDto>();

			foreach (var hotel in hotels)
			{
				var rooms = hotel.Rooms.AsEnumerable();

				if (query.GuestsCount.HasValue)
					rooms = rooms.Where(r => r.Capacity >= query.GuestsCount.Value);

				if (query.MinPrice.HasValue)
					rooms = rooms.Where(r => r.PricePerNight >= query.MinPrice.Value);

				if (query.MaxPrice.HasValue)
					rooms = rooms.Where(r => r.PricePerNight <= query.MaxPrice.Value);

				if (!string.IsNullOrEmpty(query.Accommodation))
				{
					if (Enum.TryParse<AccommodationType>(query.Accommodation, true, out var accommodationType))
						rooms = rooms.Where(r => r.Accommodation == accommodationType);
				}

				if (query.WithPets == true)
					rooms = rooms.Where(r => r.PetsAllowed || hotel.PetsAllowed || hotel.IsPetHotel);

				var filteredRooms = rooms.ToList();

				if (filteredRooms.Any())
				{
					results.Add(new HotelSearchResultDto(
						hotel.Id,
						hotel.Name,
						hotel.Description,
						hotel.Country,
						hotel.City,
						hotel.District,
						hotel.AddressLine,
						hotel.PetsAllowed,
						hotel.IsPetHotel,
						hotel.CancelFreeDaysBefore,
						filteredRooms.Select(r => new RoomSearchResultDto(
							r.Id,
							r.RoomNumber,
							r.Description,
							r.Capacity,
							r.Bedrooms,
							r.PricePerNight,
							r.PetsAllowed,
							r.Accommodation.ToString()
						)).OrderBy(r => r.PricePerNight).ToList()
					));
				}
			}

			return results.OrderBy(h => h.HotelId).ToList();
		}
	}
}
