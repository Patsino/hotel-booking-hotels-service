using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dtos
{
	public sealed record RoomSearchResultDto(
		int RoomId,
		string? RoomNumber,
		string? Description,
		int Capacity,
		int Bedrooms,
		decimal PricePerNight,
		bool PetsAllowed,
		string Accommodation);
}
