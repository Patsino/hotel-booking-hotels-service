using HotelBooking.Hotels.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
	public interface IHotelsRepository
	{
		Task<Hotel?> GetByIdAsync(int id, CancellationToken ct = default);
		Task<List<Hotel>> GetByOwnerIdAsync(int ownerId, CancellationToken ct = default);
		Task<List<Hotel>> GetPendingAsync(CancellationToken ct = default);
		Task<List<Hotel>> GetAllAsync(CancellationToken ct = default);
		Task<List<Hotel>> SearchAsync(
			string? country, string? city, string? district,
			bool? petsAllowed, bool? isPetHotelOnly,
			CancellationToken ct = default);
		Task AddAsync(Hotel hotel, CancellationToken ct = default);
		Task SaveChangesAsync(CancellationToken ct = default);
	}
}
