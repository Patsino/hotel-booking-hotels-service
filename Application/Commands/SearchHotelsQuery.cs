namespace Application.Commands
{
	public sealed record SearchHotelsQuery(
		string? Country = null,
		string? City = null,
		string? District = null,
		DateTime? StartDate = null,
		DateTime? EndDate = null,
		int? GuestsCount = null,
		bool? WithPets = null,
		bool? IsPetHotelOnly = null,
		string? Accommodation = null,
		decimal? MinPrice = null,
		decimal? MaxPrice = null);
}
