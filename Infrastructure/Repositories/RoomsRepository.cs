using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
	public sealed class RoomsRepository : IRoomsRepository
	{
		private readonly HotelsDbContext _context;

		public RoomsRepository(HotelsDbContext context) => _context = context;

		public async Task<Room?> GetByIdAsync(int id, CancellationToken ct = default)
			=> await _context.Rooms.FindAsync(new object[] { id }, ct);

		public async Task<List<Room>> GetByHotelIdAsync(int hotelId, bool includeHidden = false, CancellationToken ct = default)
		{
			var query = _context.Rooms.Where(r => r.HotelId == hotelId);
			if (!includeHidden)
				query = query.Where(r => r.Visible);
			return await query.ToListAsync(ct);
		}

		public async Task<List<Room>> SearchRoomsAsync(
			List<int> hotelIds, int? capacity, string? accommodation,
			decimal? minPrice, decimal? maxPrice, bool? petsAllowed,
			CancellationToken ct = default)
		{
			var query = _context.Rooms
				.Where(r => hotelIds.Contains(r.HotelId) && r.Visible);

			if (capacity.HasValue)
				query = query.Where(r => r.Capacity >= capacity.Value);

			if (!string.IsNullOrEmpty(accommodation))
			{
				var accommodationType = Enum.Parse<AccommodationType>(accommodation, true);
				query = query.Where(r => r.Accommodation == accommodationType);
			}

			if (minPrice.HasValue)
				query = query.Where(r => r.PricePerNight >= minPrice.Value);

			if (maxPrice.HasValue)
				query = query.Where(r => r.PricePerNight <= maxPrice.Value);

			if (petsAllowed == true)
				query = query.Where(r => r.PetsAllowed);

			return await query.ToListAsync(ct);
		}

		public async Task AddAsync(Room room, CancellationToken ct = default)
			=> await _context.Rooms.AddAsync(room, ct);

		public async Task SaveChangesAsync(CancellationToken ct = default)
			=> await _context.SaveChangesAsync(ct);

		public async Task HideRoomsByOwnerAsync(int ownerId, CancellationToken ct = default)
		{
			var hotelIds = await _context.Hotels
				.Where(h => h.OwnerId == ownerId)
				.Select(h => h.Id)
				.ToListAsync(ct);

			await _context.Rooms
				.Where(r => hotelIds.Contains(r.HotelId))
				.ExecuteUpdateAsync(r => r.SetProperty(x => x.Visible, false), ct);
		}
	}
}
