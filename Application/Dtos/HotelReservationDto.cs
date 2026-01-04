namespace Application.Dtos
{
	/// <summary>
	/// Reservation data returned from Reservations Service for hotel owner view
	/// </summary>
	public sealed record HotelReservationDto(
		int Id,
		int UserId,
		int RoomId,
		DateTime StartDate,
		DateTime EndDate,
		int GuestsCount,
		string? GuestsNames,
		string Status,
		string CancellationStatus,
		string? CancellationReason,
		DateTimeOffset? CancellationRequestedAt,
		DateTimeOffset CreatedAt);
}
