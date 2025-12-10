using Api.Filters;
using Api.Middleware;
using DotNetEnv;
using HotelBooking.Hotels.Infrastructure;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Infrastructure.Authentication;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Polly;

// Load .env file from solution root directory
var solutionRoot = Directory.GetCurrentDirectory();
// Try to find .env in current directory or parent directories
var envPath = Path.Combine(solutionRoot, ".env");
if (!File.Exists(envPath))
{
    // If running from Api folder, go up one level
    var parentDir = Directory.GetParent(solutionRoot)?.FullName;
    if (parentDir != null)
    {
        envPath = Path.Combine(parentDir, ".env");
    }
}

if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine($"Warning: .env file not found. Looking in: {solutionRoot}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Hotel Booking - Hotels Service API",
		Version = "v1.0.0",
		Description = @"Hotels microservice for managing hotels and rooms.

**Features:**
- Create and manage hotels (HotelOwner/Admin)
- Search approved hotels by location, dates, capacity, pets
- Manage rooms: create, update, hide/show
- Admin approval workflow for new hotels

**Authentication:** JWT Bearer token required for protected endpoints.

**Roles:** User, HotelOwner, Admin",
		Contact = new OpenApiContact
		{
			Name = "Hotel Booking System",
			Email = "support@hotelbooking.com"
		}
	});

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

	var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	if (File.Exists(xmlPath))
	{
		c.IncludeXmlComments(xmlPath);
	}

	c.EnableAnnotations();
	c.SchemaFilter<ExampleSchemaFilter>();
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

// Skip migration and seeding in Testing environment (InMemory database)
if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
	using (var scope = app.Services.CreateScope())
	{
		var services = scope.ServiceProvider;
		var logger = services.GetRequiredService<ILogger<Program>>();

		try
		{
			var dbContext = services.GetRequiredService<HotelsDbContext>();

			// For Production (Azure Free tier with cold start), use retry logic with longer timeout
			if (app.Environment.IsProduction())
			{
				logger.LogInformation("Production environment detected. Using retry policy for cold database start...");

				// Configure retry policy with exponential backoff for cold Azure DB
				var retryPolicy = Policy
					.Handle<Exception>()
					.WaitAndRetryAsync(
						retryCount: 5,
						sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8, 16, 32 sec
						onRetry: (exception, timeSpan, retryCount, context) =>
						{
							logger.LogWarning(
								"Database connection attempt {RetryCount} failed. Waiting {WaitSeconds}s before next retry. Error: {Error}",
								retryCount,
								timeSpan.TotalSeconds,
								exception.Message);
						});

				// Set longer command timeout for cold database (default is 30 sec)
				dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(90));

				await retryPolicy.ExecuteAsync(async () =>
				{
					logger.LogInformation("Starting database migration...");
					await dbContext.Database.MigrateAsync();
					logger.LogInformation("Database migration completed");
				});

				await retryPolicy.ExecuteAsync(async () =>
				{
					logger.LogInformation("Starting database seeding...");
					var seeder = services.GetRequiredService<HotelsDataSeeder>();
					await seeder.SeedAsync();
					logger.LogInformation("Database seeding completed");
				});
			}
			else
			{
				// Development/Testing: use default behavior (fast local DB)
				logger.LogInformation("Starting database migration...");
				await dbContext.Database.MigrateAsync();
				logger.LogInformation("Database migration completed");

				logger.LogInformation("Starting database seeding...");
				var seeder = services.GetRequiredService<HotelsDataSeeder>();
				await seeder.SeedAsync();
				logger.LogInformation("Database seeding completed");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred during migration or seeding");
			throw;
		}
	}
}

app.UseExceptionHandler();

// Register correlation ID middleware EARLY in the pipeline
app.UseMiddleware<Api.Middleware.CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
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

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }