namespace Application.Dtos
{
	public sealed record HotelSearchResultDto(
		int HotelId,
		string HotelName,
		string? Description,
		string Country,
		string City,
		string? District,
		string? AddressLine,
		bool PetsAllowed,
		bool IsPetHotel,
		int CancelFreeDaysBefore,
		List<RoomSearchResultDto> Rooms);
}
