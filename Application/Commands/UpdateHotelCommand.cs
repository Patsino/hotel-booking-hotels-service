using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
	public sealed record UpdateHotelCommand(
		[Required][MaxLength(255)] string Name,
		string? Description,
		[MaxLength(120)] string? District,
		[MaxLength(300)] string? AddressLine,
		bool PetsAllowed,
		bool IsPetHotel,
		[Range(0, 30)] int CancelFreeDaysBefore);
}
