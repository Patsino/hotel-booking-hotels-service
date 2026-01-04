using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
