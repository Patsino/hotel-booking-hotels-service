using HotelBooking.Hotels.Domain.Hotels;

namespace Application.Services
{
	public interface IRoomsRepository
	{
		Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
		Task<List<Room>> GetByHotelIdAsync(int hotelId, bool includeHidden = false, CancellationToken ct = default);
		Task AddAsync(Room room, CancellationToken ct = default);
		Task SaveChangesAsync(CancellationToken ct = default);
		Task HideRoomsByOwnerAsync(int ownerId, CancellationToken ct = default);
	}
}
