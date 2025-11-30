namespace HotelBooking.Hotels.Domain.Hotels;

public sealed class Hotel
{
	public int Id { get; private set; }
	public int OwnerId { get; private set; }
	public string Name { get; private set; } = null!;
	public string? Description { get; private set; }
	public string? MainImageUrl { get; private set; }
	public string Country { get; private set; } = null!;
	public string City { get; private set; } = null!;
	public string? District { get; private set; }
	public string? AddressLine { get; private set; }
	public bool PetsAllowed { get; private set; }
	public bool IsPetHotel { get; private set; }
	public int CancelFreeDaysBefore { get; private set; }
	public ApprovalStatus Approval { get; private set; } = ApprovalStatus.Pending;
	public DateTimeOffset SubmittedAt { get; private set; }
	public DateTimeOffset? ReviewedAt { get; private set; }

	public Hotel(int ownerId, string name, string country, string city)
	{
		OwnerId = ownerId;
		Name = name;
		Country = country;
		City = city;
		CancelFreeDaysBefore = 3;
		SubmittedAt = DateTimeOffset.UtcNow;
	}

	private Hotel() { }

	public void Update(string name, string? description, string? district,
		string? addressLine, bool petsAllowed, bool isPetHotel, int cancelFreeDaysBefore)
	{
		Name = name;
		Description = description;
		District = district;
		AddressLine = addressLine;
		PetsAllowed = petsAllowed;
		IsPetHotel = isPetHotel;
		CancelFreeDaysBefore = cancelFreeDaysBefore;
	}

	public void Submit()
	{
		Approval = ApprovalStatus.Pending;
		SubmittedAt = DateTimeOffset.UtcNow;
	}

	public void Approve()
	{
		Approval = ApprovalStatus.Approved;
		ReviewedAt = DateTimeOffset.UtcNow;
	}

	public void Reject()
	{
		Approval = ApprovalStatus.Rejected;
		ReviewedAt = DateTimeOffset.UtcNow;
	}
}
