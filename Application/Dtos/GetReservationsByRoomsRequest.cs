namespace Application.Dtos
{
	/// <summary>
	/// Request to get reservations for a list of room IDs
	/// </summary>
	public sealed record GetReservationsByRoomsRequest(List<int> RoomIds);
}
