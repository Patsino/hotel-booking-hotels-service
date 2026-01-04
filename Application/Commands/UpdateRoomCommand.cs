using System.ComponentModel.DataAnnotations;

namespace Application.Commands
{
	public sealed record UpdateRoomCommand(
		[MaxLength(50)] string? RoomNumber,
		string? Description,
		[Range(1, 20)] int Capacity,
		[Range(1, 10)] int Bedrooms,
		[Range(0.01, double.MaxValue)] decimal PricePerNight,
		bool PetsAllowed,
		string Accommodation);
}
