using Application.Commands;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;

namespace Application.Handlers
{
	public sealed class CreateRoomHandler
	{
		private readonly IRoomsRepository _repository;
		private readonly IHotelsRepository _hotelsRepository;

		public CreateRoomHandler(IRoomsRepository repository, IHotelsRepository hotelsRepository)
		{
			_repository = repository;
			_hotelsRepository = hotelsRepository;
		}

		public async Task<int> HandleAsync(CreateRoomCommand command, CancellationToken ct = default)
		{
			var hotel = await _hotelsRepository.GetByIdAsync(command.HotelId, ct);
			if (hotel == null)
			{
				throw new InvalidOperationException("Hotel not found");
			}

			// Validate accommodation type
			if (!Enum.TryParse<AccommodationType>(command.Accommodation, ignoreCase: true, out var accommodation))
			{
				throw new InvalidOperationException($"Invalid accommodation type: {command.Accommodation}");
			}

			var room = new Room(command.HotelId, command.Capacity, command.Bedrooms, command.PricePerNight);

			await _repository.AddAsync(room, ct);
			await _repository.SaveChangesAsync(ct);

			return room.Id;
		}
	}
}
