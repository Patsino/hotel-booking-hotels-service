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

		public async Task<List<Hotel>> SearchAsync(
			string? country, string? city, string? district,
			bool? petsAllowed, bool? isPetHotelOnly,
			CancellationToken ct = default)
		{
			var query = _context.Hotels.Where(h => h.Approval == ApprovalStatus.Approved);

			if (!string.IsNullOrEmpty(country))
				query = query.Where(h => h.Country == country);

			if (!string.IsNullOrEmpty(city))
				query = query.Where(h => h.City == city);

			if (!string.IsNullOrEmpty(district))
				query = query.Where(h => h.District == district);

			if (petsAllowed == true)
				query = query.Where(h => h.PetsAllowed || h.IsPetHotel);

			if (isPetHotelOnly == true)
				query = query.Where(h => h.IsPetHotel);

			return await query.ToListAsync(ct);
		}

		public async Task AddAsync(Hotel hotel, CancellationToken ct = default)
			=> await _context.Hotels.AddAsync(hotel, ct);

		public async Task SaveChangesAsync(CancellationToken ct = default)
			=> await _context.SaveChangesAsync(ct);
	}
}
