using Application.Handlers;
using Application.Services;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Http;
using Infrastructure.Repositories;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

		services.AddHttpClient("ReservationsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Reservations"] ?? "http://localhost:5003";
			client.BaseAddress = new Uri(baseUrl);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

		services.AddHttpClient("HotelsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Hotels"] ?? "http://localhost:5002";
			client.BaseAddress = new Uri(baseUrl);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

		services.AddHttpClient("PaymentsService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Payments"] ?? "http://localhost:5004";
			client.BaseAddress = new Uri(baseUrl);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

		services.AddHttpClient("UsersService", client =>
		{
			var baseUrl = configuration["ServiceUrls:Users"] ?? "http://localhost:5001";
			client.BaseAddress = new Uri(baseUrl);
		})
		.AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserService, CurrentUserService>();
		services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

		services.AddScoped<HotelsDataSeeder>();

		return services;
	}
}
