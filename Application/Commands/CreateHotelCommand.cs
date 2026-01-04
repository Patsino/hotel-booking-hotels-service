using System.ComponentModel.DataAnnotations;

namespace Application.Commands
{
	public sealed record CreateHotelCommand(
		[Required] int OwnerId,
		[Required][MaxLength(255)] string Name,
		[Required][MaxLength(100)] string Country,
		[Required][MaxLength(120)] string City,
		string? Description = null,
		[MaxLength(120)] string? District = null,
		[MaxLength(300)] string? AddressLine = null,
		bool PetsAllowed = false,
		bool IsPetHotel = false,
		[Range(0, 30)] int CancelFreeDaysBefore = 3);
}
