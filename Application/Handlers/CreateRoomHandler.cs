using Application.Commands;
using Application.Services;
using HotelBooking.Hotels.Domain.Hotels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

			var accommodation = Enum.Parse<AccommodationType>(command.Accommodation, ignoreCase: true);
			var room = new Room(command.HotelId, command.Capacity, command.Bedrooms, command.PricePerNight);

			await _repository.AddAsync(room, ct);
			await _repository.SaveChangesAsync(ct);

			return room.Id;
		}
	}
}
