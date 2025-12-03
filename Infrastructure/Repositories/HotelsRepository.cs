using Application.Commands;
using Application.Dtos;
using Application.Services;
using Dapper;
using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Repositories.Rows;
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

		public async Task<List<Hotel>> GetAllAsync(CancellationToken ct = default)
			=> await _context.Hotels.OrderByDescending(h => h.SubmittedAt).ToListAsync(ct);

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

		public async Task<List<HotelSearchResultDto>> SearchAvailableHotelsAsync(
			SearchHotelsQuery query,
			CancellationToken ct = default)
		{
			// Build SQL query that joins across schemas in a SINGLE database call
			var sql = @"
				WITH AvailableRooms AS (
					SELECT 
						h.Id AS HotelId,
						h.Name AS HotelName,
						h.Description AS HotelDescription,
						h.Country,
						h.City,
						h.District,
						h.AddressLine,
						h.PetsAllowed AS HotelPetsAllowed,
						h.IsPetHotel,
						h.CancelFreeDaysBefore,
						r.Id AS RoomId,
						r.RoomNumber,
						r.Description AS RoomDescription,
						r.Capacity,
						r.Bedrooms,
						r.PricePerNight,
						r.PetsAllowed AS RoomPetsAllowed,
						r.Accommodation
					FROM hotels.Hotels h
					INNER JOIN hotels.Rooms r ON h.Id = r.HotelId
					WHERE 
						h.Approval = 'Approved'
						AND r.Visible = 1
						AND (@Country IS NULL OR h.Country = @Country)
						AND (@City IS NULL OR h.City = @City)
						AND (@District IS NULL OR h.District = @District)
						AND (@Capacity IS NULL OR r.Capacity >= @Capacity)
						AND (@MinPrice IS NULL OR r.PricePerNight >= @MinPrice)
						AND (@MaxPrice IS NULL OR r.PricePerNight <= @MaxPrice)
						AND (@Accommodation IS NULL OR r.Accommodation = @Accommodation)
						AND (@WithPets = 0 OR r.PetsAllowed = 1 OR h.PetsAllowed = 1 OR h.IsPetHotel = 1)
						AND (@IsPetHotelOnly = 0 OR h.IsPetHotel = 1)
				),
				RoomAvailability AS (
					SELECT 
						ar.*,
						CASE 
							WHEN @StartDate IS NULL OR @EndDate IS NULL THEN 1
							WHEN EXISTS (
								SELECT 1 
								FROM reservations.Reservations res
								WHERE res.RoomId = ar.RoomId
									AND res.Status != 'Canceled'
									AND res.StartDate < @EndDate
									AND res.EndDate > @StartDate
							) THEN 0
							ELSE 1
						END AS IsAvailable
					FROM AvailableRooms ar
				)
				SELECT *
				FROM RoomAvailability
				WHERE IsAvailable = 1
				ORDER BY HotelId, PricePerNight";

			var parameters = new
			{
				Country = query.Country,
				City = query.City,
				District = query.District,
				Capacity = query.GuestsCount,
				MinPrice = query.MinPrice,
				MaxPrice = query.MaxPrice,
				Accommodation = query.Accommodation,
				WithPets = query.WithPets ?? false,
				IsPetHotelOnly = query.IsPetHotelOnly ?? false,
				StartDate = query.StartDate,
				EndDate = query.EndDate
			};

			List<HotelRoomRow> results;

			using (var connection = _context.Database.GetDbConnection())
			{
				await connection.OpenAsync(ct);
				results = (await connection.QueryAsync<HotelRoomRow>(sql, parameters))
					.ToList();
			}

			// Group by hotel
			var hotels = results
				.GroupBy(r => r.HotelId)
				.Select(g =>
				{
					var first = g.First();
					return new HotelSearchResultDto(
						first.HotelId,
						first.HotelName,
						first.HotelDescription,
						first.Country,
						first.City,
						first.District,
						first.AddressLine,
						first.HotelPetsAllowed,
						first.IsPetHotel,
						first.CancelFreeDaysBefore,
						g.Select(r => new RoomSearchResultDto(
							r.RoomId,
							r.RoomNumber,
							r.RoomDescription,
							r.Capacity,
							r.Bedrooms,
							r.PricePerNight,
							r.RoomPetsAllowed,
							r.Accommodation
						)).ToList()
					);
				})
				.ToList();

			return hotels;
		}
	}
}
