using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Http;
using Infrastructure.Repositories;
using Infrastructure.Seeding;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace HotelBooking.Hotels.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? environmentName = null)
    {
        if (environmentName == "Testing")
        {
            services.AddDbContext<HotelsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
        }
        else
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HotelBookingDatabase");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("HotelBookingDatabase") ??
                    throw new InvalidOperationException("ConnectionStrings:HotelBookingDatabase is not configured.");
            }

            services.AddDbContext<HotelsDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "hotels");
                });
            });
        }

		services.AddScoped<IHotelsRepository, HotelsRepository>();
		services.AddScoped<IRoomsRepository, RoomsRepository>();

		services.AddScoped<CreateHotelHandler>();
		services.AddScoped<CreateRoomHandler>();

		services.AddTransient<AuthenticatedHttpClientHandler>();

		// Configure Polly retry and timeout policies for resilience
		var retryPolicy = HttpPolicyExtensions
			.HandleTransientHttpError() // Handles 5xx and 408
			.WaitAndRetryAsync(
				retryCount: 3,
				sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
				onRetry: (outcome, timespan, retryAttempt, context) =>
				{
					Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
				});

		var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

		services.AddHttpClient("ReservationsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Reservations"] ?? "http://localhost:5003";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(100);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("HotelsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Hotels"] ?? "http://localhost:5002";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(100);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("PaymentsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Payments"] ?? "http://localhost:5004";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(100);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("UsersService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Users"] ?? "http://localhost:5001";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(100);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

		services.AddScoped<HotelsDataSeeder>();

		return services;
	}
}
