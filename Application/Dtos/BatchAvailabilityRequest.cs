namespace Application.Dtos
{
	/// <summary>
	/// Request to check availability for multiple rooms in a date range
	/// </summary>
	public sealed record BatchAvailabilityRequest(
		List<int> RoomIds,
		DateTime StartDate,
		DateTime EndDate);
}
