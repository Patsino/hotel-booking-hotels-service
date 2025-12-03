using HotelBooking.Hotels.Infrastructure.Persistence;
using HotelBooking.Hotels.Infrastructure;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Authentication;
using Infrastructure.Seeding;
using Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// ADD AUTHENTICATION
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Hotel Booking - Hotels API",
		Version = "v1"
	});

	// ADD SWAGGER AUTH
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddHealthChecks()
	.AddDbContextCheck<HotelsDbContext>();

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
	});
});

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//	var dbContext = scope.ServiceProvider.GetRequiredService<HotelsDbContext>();
//	dbContext.Database.Migrate();
//}

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var logger = services.GetRequiredService<ILogger<Program>>();

	try
	{
		logger.LogInformation("Starting database migration...");
		var dbContext = services.GetRequiredService<HotelsDbContext>();
		await dbContext.Database.MigrateAsync();
		logger.LogInformation("Database migration completed");

		logger.LogInformation("Starting database seeding...");
		var seeder = services.GetRequiredService<HotelsDataSeeder>();
		await seeder.SeedAsync();
		logger.LogInformation("Database seeding completed");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "An error occurred during migration or seeding");
		throw;
	}
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();