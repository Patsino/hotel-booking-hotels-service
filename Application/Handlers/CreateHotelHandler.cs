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
	public sealed class CreateHotelHandler
	{
		private readonly IHotelsRepository _repository;

		public CreateHotelHandler(IHotelsRepository repository)
		{
			_repository = repository;
		}

		public async Task<int> HandleAsync(CreateHotelCommand command, CancellationToken ct = default)
		{
			var hotel = new Hotel(command.OwnerId, command.Name, command.Country, command.City);

			await _repository.AddAsync(hotel, ct);
			await _repository.SaveChangesAsync(ct);

			return hotel.Id;
		}
	}
}
