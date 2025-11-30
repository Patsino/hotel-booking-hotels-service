using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
	public sealed record CreateRoomCommand(
		[Required] int HotelId,
		[Range(1, 20)] int Capacity,
		[Range(1, 10)] int Bedrooms,
		[Range(0.01, double.MaxValue)] decimal PricePerNight,
		[MaxLength(50)] string? RoomNumber = null,
		string? Description = null,
		bool PetsAllowed = false,
		string Accommodation = "HotelRoom");
}
