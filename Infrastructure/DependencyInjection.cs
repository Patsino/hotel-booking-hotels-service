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
using System.Diagnostics.CodeAnalysis;

namespace HotelBooking.Hotels.Infrastructure;
[ExcludeFromCodeCoverage]

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
            var connectionString = GetConfigValue(configuration, "ConnectionStrings:HotelBookingDatabase", "ConnectionStrings__HotelBookingDatabase")
                ?? throw new InvalidOperationException("ConnectionStrings:HotelBookingDatabase is not configured.");

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


	var retryPolicy = HttpPolicyExtensions
		.HandleTransientHttpError() 
		.WaitAndRetryAsync(
			retryCount: 3,
			sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(0.5 * Math.Pow(2, retryAttempt)),
			onRetry: (outcome, timespan, retryAttempt, context) =>
			{
				Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
			});

	var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));		services.AddHttpClient("ReservationsService", client =>
		{
			var baseUrl = GetConfigValue(configuration, "ServiceUrls:Reservations", "ServiceUrls__Reservations") 
				?? "http://reservations-service:8080";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("HotelsService", client =>
		{
			var baseUrl = GetConfigValue(configuration, "ServiceUrls:Hotels", "ServiceUrls__Hotels") 
				?? "http://hotels-service:8080";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("PaymentsService", client =>
		{
			var baseUrl = GetConfigValue(configuration, "ServiceUrls:Payments", "ServiceUrls__Payments") 
				?? "http://payments-service:8080";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpClient("UsersService", client =>
		{
			var baseUrl = GetConfigValue(configuration, "ServiceUrls:Users", "ServiceUrls__Users") 
				?? "http://users-service:8080";
			client.BaseAddress = new Uri(baseUrl);
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
		.AddPolicyHandler(retryPolicy)
		.AddPolicyHandler(timeoutPolicy);

		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

		// HTTP client services for cross-service communication
		services.AddScoped<IReservationsServiceClient, ReservationsServiceClient>();

		services.AddScoped<HotelsDataSeeder>();

		return services;
	}

	/// <summary>
	/// Gets configuration value from environment variable first, then falls back to IConfiguration.
	/// </summary>
	/// <param name="configuration">The configuration instance</param>
	/// <param name="configKey">The IConfiguration key (e.g., "ServiceUrls:Reservations")</param>
	/// <param name="envKey">The environment variable key (e.g., "ServiceUrls__Reservations")</param>
	/// <returns>The configuration value or null if not found</returns>
	private static string? GetConfigValue(IConfiguration configuration, string configKey, string envKey)
	{
		var envValue = Environment.GetEnvironmentVariable(envKey);
		if (!string.IsNullOrWhiteSpace(envValue))
		{
			return envValue;
		}

		return configuration[configKey];
	}
}
