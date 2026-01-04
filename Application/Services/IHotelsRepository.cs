using Application.Commands;
using Application.Dtos;
using HotelBooking.Hotels.Domain.Hotels;

namespace Application.Services
{
	public interface IHotelsRepository
	{
		Task<Hotel?> GetByIdAsync(int id, CancellationToken ct = default);
		Task<List<Hotel>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default);
		Task<List<Hotel>> GetPendingAsync(CancellationToken ct = default);
		Task<List<Hotel>> GetAllAsync(CancellationToken ct = default);
		Task AddAsync(Hotel hotel, CancellationToken ct = default);
		Task SaveChangesAsync(CancellationToken ct = default);
		
		/// <summary>
		/// Searches for hotels with available rooms based on filters (without availability check).
		/// Use in combination with IReservationsServiceClient for availability filtering.
		/// </summary>
		Task<List<HotelSearchResultDto>> SearchHotelsWithRoomsAsync(SearchHotelsQuery query, CancellationToken ct = default);
	}
}
