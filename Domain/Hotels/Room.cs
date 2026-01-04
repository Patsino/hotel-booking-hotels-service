namespace HotelBooking.Hotels.Domain.Hotels;

public sealed class Room
{
	public int Id { get; private set; }
	public int HotelId { get; private set; }
	public string? RoomNumber { get; private set; }
	public string? Description { get; private set; }
	public int Capacity { get; private set; }
	public int Bedrooms { get; private set; }
	public decimal PricePerNight { get; private set; }
	public string? MainImageUrl { get; private set; }
	public bool Visible { get; private set; }
	public bool PetsAllowed { get; private set; }
	public AccommodationType Accommodation { get; private set; }
	public DateTimeOffset CreatedAt { get; private set; }

	public Room(int hotelId, int capacity, int bedrooms, decimal pricePerNight)
	{
		HotelId = hotelId;
		Capacity = capacity;
		Bedrooms = bedrooms;
		PricePerNight = pricePerNight;
		Visible = true;
		Accommodation = AccommodationType.HotelRoom;
		CreatedAt = DateTimeOffset.UtcNow;
	}

	private Room() { }

	public void Update(string? roomNumber, string? description, int capacity,
		int bedrooms, decimal pricePerNight, bool petsAllowed, AccommodationType accommodation)
	{
		RoomNumber = roomNumber;
		Description = description;
		Capacity = capacity;
		Bedrooms = bedrooms;
		PricePerNight = pricePerNight;
		PetsAllowed = petsAllowed;
		Accommodation = accommodation;
	}

	public void Hide() => Visible = false;
	public void Show() => Visible = true;
}