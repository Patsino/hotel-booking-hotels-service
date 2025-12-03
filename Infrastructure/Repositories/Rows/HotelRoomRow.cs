using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Rows
{
	sealed class HotelRoomRow
	{
		public int HotelId { get; set; }
		public string HotelName { get; set; } = null!;
		public string? HotelDescription { get; set; }
		public string Country { get; set; } = null!;
		public string City { get; set; } = null!;
		public string? District { get; set; }
		public string? AddressLine { get; set; }
		public bool HotelPetsAllowed { get; set; }
		public bool IsPetHotel { get; set; }
		public int CancelFreeDaysBefore { get; set; }
		public int RoomId { get; set; }
		public string? RoomNumber { get; set; }
		public string? RoomDescription { get; set; }
		public int Capacity { get; set; }
		public int Bedrooms { get; set; }
		public decimal PricePerNight { get; set; }
		public bool RoomPetsAllowed { get; set; }
		public string Accommodation { get; set; } = null!;
	}
}
