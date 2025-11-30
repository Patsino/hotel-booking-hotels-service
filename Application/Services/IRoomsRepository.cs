using HotelBooking.Hotels.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
	public interface IRoomsRepository
	{
		Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
		Task<List<Room>> GetByHotelIdAsync(int hotelId, bool includeHidden = false, CancellationToken ct = default);
		Task<List<Room>> SearchRoomsAsync(
			List<int> hotelIds,
			int? capacity, string? accommodation,
			decimal? minPrice, decimal? maxPrice,
			bool? petsAllowed,
			CancellationToken ct = default);
		Task AddAsync(Room room, CancellationToken ct = default);
		Task SaveChangesAsync(CancellationToken ct = default);
		Task HideRoomsByOwnerAsync(int ownerId, CancellationToken ct = default);
	}
}
