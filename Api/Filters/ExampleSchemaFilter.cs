using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Application.Commands;
using Application.Dtos;

namespace Api.Filters;

public class ExampleSchemaFilter : ISchemaFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (context.Type == typeof(CreateHotelCommand))
		{
			schema.Example = new OpenApiObject
			{
				["ownerId"] = new OpenApiInteger(1),
				["name"] = new OpenApiString("Grand Hotel Plaza"),
				["country"] = new OpenApiString("France"),
				["city"] = new OpenApiString("Paris"),
				["description"] = new OpenApiString("Luxury hotel in the heart of Paris with stunning views of the Eiffel Tower"),
				["district"] = new OpenApiString("7th Arrondissement"),
				["addressLine"] = new OpenApiString("123 Rue de la Convention"),
				["petsAllowed"] = new OpenApiBoolean(true),
				["isPetHotel"] = new OpenApiBoolean(false),
				["cancelFreeDaysBefore"] = new OpenApiInteger(7)
			};
		}
		else if (context.Type == typeof(UpdateHotelCommand))
		{
			schema.Example = new OpenApiObject
			{
				["hotelId"] = new OpenApiInteger(42),
				["name"] = new OpenApiString("Grand Hotel Plaza"),
				["description"] = new OpenApiString("Updated description with new amenities"),
				["district"] = new OpenApiString("7th Arrondissement"),
				["addressLine"] = new OpenApiString("123 Rue de la Convention"),
				["petsAllowed"] = new OpenApiBoolean(true),
				["isPetHotel"] = new OpenApiBoolean(false),
				["cancelFreeDaysBefore"] = new OpenApiInteger(7)
			};
		}
		else if (context.Type == typeof(CreateRoomCommand))
		{
			schema.Example = new OpenApiObject
			{
				["hotelId"] = new OpenApiInteger(42),
				["capacity"] = new OpenApiInteger(2),
				["bedrooms"] = new OpenApiInteger(1),
				["pricePerNight"] = new OpenApiDouble(150.00),
				["roomNumber"] = new OpenApiString("101"),
				["description"] = new OpenApiString("Deluxe room with king bed and city view"),
				["petsAllowed"] = new OpenApiBoolean(true),
				["accommodation"] = new OpenApiString("HotelRoom")
			};
		}
		else if (context.Type == typeof(UpdateRoomCommand))
		{
			schema.Example = new OpenApiObject
			{
				["roomId"] = new OpenApiInteger(15),
				["capacity"] = new OpenApiInteger(4),
				["bedrooms"] = new OpenApiInteger(2),
				["pricePerNight"] = new OpenApiDouble(250.00),
				["roomNumber"] = new OpenApiString("201"),
				["description"] = new OpenApiString("Family suite with two queen beds and balcony"),
				["petsAllowed"] = new OpenApiBoolean(false),
				["accommodation"] = new OpenApiString("Suite")
			};
		}
		else if (context.Type == typeof(SearchHotelsQuery))
		{
			schema.Example = new OpenApiObject
			{
				["country"] = new OpenApiString("France"),
				["city"] = new OpenApiString("Paris"),
				["startDate"] = new OpenApiString("2024-12-20"),
				["endDate"] = new OpenApiString("2024-12-25"),
				["guestsCount"] = new OpenApiInteger(2),
				["minPrice"] = new OpenApiDouble(50.00),
				["maxPrice"] = new OpenApiDouble(500.00),
				["petsAllowed"] = new OpenApiBoolean(false)
			};
		}
		else if (context.Type == typeof(HotelSearchResultDto))
		{
			schema.Example = new OpenApiObject
			{
				["hotelId"] = new OpenApiInteger(42),
				["name"] = new OpenApiString("Grand Hotel Plaza"),
				["city"] = new OpenApiString("Paris"),
				["country"] = new OpenApiString("France"),
				["description"] = new OpenApiString("Luxury hotel in the heart of Paris"),
				["petsAllowed"] = new OpenApiBoolean(true),
				["isPetHotel"] = new OpenApiBoolean(false),
				["minPricePerNight"] = new OpenApiDouble(150.00),
				["availableRooms"] = new OpenApiInteger(5)
			};
		}
		else if (context.Type == typeof(RoomSearchResultDto))
		{
			schema.Example = new OpenApiObject
			{
				["roomId"] = new OpenApiInteger(15),
				["hotelId"] = new OpenApiInteger(42),
				["hotelName"] = new OpenApiString("Grand Hotel Plaza"),
				["capacity"] = new OpenApiInteger(2),
				["bedrooms"] = new OpenApiInteger(1),
				["pricePerNight"] = new OpenApiDouble(150.00),
				["roomNumber"] = new OpenApiString("101"),
				["description"] = new OpenApiString("Deluxe room with king bed"),
				["petsAllowed"] = new OpenApiBoolean(true),
				["accommodation"] = new OpenApiString("HotelRoom")
			};
		}
	}
}
