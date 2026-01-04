namespace Application.Dtos
{
	/// <summary>
	/// Response containing IDs of rooms that are NOT available (have overlapping reservations)
	/// </summary>
	public sealed record BatchAvailabilityResponse(
		List<int> UnavailableRoomIds);
}
