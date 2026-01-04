using Application.Dtos;
using Application.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Infrastructure.Http
{
	public sealed class ReservationsServiceClient : IReservationsServiceClient
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ILogger<ReservationsServiceClient> _logger;

		public ReservationsServiceClient(
			IHttpClientFactory httpClientFactory,
			ILogger<ReservationsServiceClient> logger)
		{
			_httpClientFactory = httpClientFactory;
			_logger = logger;
		}

		public async Task<List<int>> GetUnavailableRoomIdsAsync(
			List<int> roomIds,
			DateTime startDate,
			DateTime endDate,
			CancellationToken ct = default)
		{
			if (roomIds == null || roomIds.Count == 0)
			{
				return new List<int>();
			}

			try
			{
				var client = _httpClientFactory.CreateClient("ReservationsService");
				var request = new BatchAvailabilityRequest(roomIds, startDate, endDate);

				_logger.LogDebug(
					"Checking availability for {RoomCount} rooms from {StartDate} to {EndDate}",
					roomIds.Count, startDate, endDate);

				var response = await client.PostAsJsonAsync("/internal/reservations/batch-availability", request, ct);

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning(
						"Batch availability check failed with status {StatusCode}",
						response.StatusCode);
					// On failure, return empty list (assume all rooms available) to not block search
					// This is a graceful degradation - availability will be checked again at booking time
					return new List<int>();
				}

				var result = await response.Content.ReadFromJsonAsync<BatchAvailabilityResponse>(ct);
				
				_logger.LogDebug(
					"Batch availability check returned {UnavailableCount} unavailable rooms",
					result?.UnavailableRoomIds?.Count ?? 0);

				return result?.UnavailableRoomIds ?? new List<int>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error checking batch availability for {RoomCount} rooms", roomIds.Count);
				// On error, return empty list (assume all rooms available) to not block search
				// Availability will be verified again when creating the reservation
				return new List<int>();
			}
		}

		public async Task<List<HotelReservationDto>> GetReservationsByRoomIdsAsync(
			List<int> roomIds,
			CancellationToken ct = default)
		{
			if (roomIds == null || roomIds.Count == 0)
			{
				return new List<HotelReservationDto>();
			}

			try
			{
				var client = _httpClientFactory.CreateClient("ReservationsService");
				var request = new GetReservationsByRoomsRequest(roomIds);

				_logger.LogDebug(
					"Getting reservations for {RoomCount} rooms",
					roomIds.Count);

				var response = await client.PostAsJsonAsync("/internal/reservations/by-rooms", request, ct);

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning(
						"Get reservations by rooms failed with status {StatusCode}",
						response.StatusCode);
					return new List<HotelReservationDto>();
				}

				var result = await response.Content.ReadFromJsonAsync<List<HotelReservationDto>>(ct);
				
				_logger.LogDebug(
					"Get reservations by rooms returned {ReservationCount} reservations",
					result?.Count ?? 0);

				return result ?? new List<HotelReservationDto>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting reservations for {RoomCount} rooms", roomIds.Count);
				return new List<HotelReservationDto>();
			}
		}
	}
}
