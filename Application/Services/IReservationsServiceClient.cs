using Application.Dtos;

namespace Application.Services
{
	/// <summary>
	/// Service for checking room availability via Reservations service
	/// </summary>
	public interface IReservationsServiceClient
	{
		/// <summary>
		/// Checks availability for a batch of rooms in a date range.
		/// Returns IDs of rooms that are NOT available (have overlapping reservations).
		/// </summary>
		Task<List<int>> GetUnavailableRoomIdsAsync(
			List<int> roomIds,
			DateTime startDate,
			DateTime endDate,
			CancellationToken ct = default);

		/// <summary>
		/// Gets all reservations for the specified room IDs.
		/// Used by hotel owners to view reservations on their properties.
		/// </summary>
		Task<List<HotelReservationDto>> GetReservationsByRoomIdsAsync(
			List<int> roomIds,
			CancellationToken ct = default);
	}
}
