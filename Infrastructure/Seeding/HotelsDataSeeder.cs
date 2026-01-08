using HotelBooking.Hotels.Domain.Hotels;
using HotelBooking.Hotels.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Seeding
{
    [ExcludeFromCodeCoverage]
    public sealed class HotelsDataSeeder
    {
        private readonly HotelsDbContext _context;
        private readonly ILogger<HotelsDataSeeder> _logger;

        public HotelsDataSeeder(HotelsDbContext context, ILogger<HotelsDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            if (await _context.Hotels.AnyAsync())
            {
                _logger.LogInformation("Hotels already seeded, skipping");
                return;
            }

            _logger.LogInformation("Seeding hotels and rooms...");

            // Hotel 1 - Grand Hotel Vilnius (Owner 1, Approved)
            var hotel1 = new Hotel(2, "Grand Hotel Vilnius", "Lithuania", "Vilnius");
            hotel1.Update(
                "Grand Hotel Vilnius",
                "Luxury hotel in the heart of Vilnius Old Town",
                "Old Town",
                "Gedimino Ave 1",
                false,
                false,
                7);
            hotel1.Approve(); // Pre-approved for testing

            await _context.Hotels.AddAsync(hotel1);
            await _context.SaveChangesAsync();

            var hotel1Rooms = new List<Room>
        {
            CreateRoom(hotel1.Id, "101", "Deluxe Single Room", 1, 1, 89.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel1.Id, "102", "Standard Double Room", 2, 1, 129.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel1.Id, "103", "Family Suite", 4, 2, 199.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel1.Id, "201", "Executive Suite", 2, 1, 249.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel1.Id, "202", "Presidential Suite", 4, 3, 499.99m, AccommodationType.HotelRoom)
        };

            // Hotel 2 - Seaside Resort Klaipeda (Owner 2, Approved, Pet-friendly)
            var hotel2 = new Hotel(3, "Seaside Resort Klaipeda", "Lithuania", "Klaipeda");
            hotel2.Update(
                "Seaside Resort Klaipeda",
                "Beautiful beachfront resort with sea views",
                "Smiltyne",
                "Beach Street 10",
                true, // Pets allowed
                false,
                5);
            hotel2.Approve();

            await _context.Hotels.AddAsync(hotel2);
            await _context.SaveChangesAsync();

            var hotel2Rooms = new List<Room>
        {
            CreateRoom(hotel2.Id, "A1", "Sea View Room", 2, 1, 159.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel2.Id, "A2", "Beach Bungalow", 3, 2, 219.99m, AccommodationType.Cabin, true),
            CreateRoom(hotel2.Id, "B1", "Family Villa", 6, 3, 349.99m, AccommodationType.House, true),
            CreateRoom(hotel2.Id, "B2", "Luxury Apartment", 4, 2, 279.99m, AccommodationType.Apartment, true)
        };

            // Hotel 3 - Pet Paradise Hotel (Owner 2, Approved, Dedicated pet hotel)
            var hotel3 = new Hotel(3, "Pet Paradise Hotel", "Lithuania", "Vilnius");
            hotel3.Update(
                "Pet Paradise Hotel",
                "Special hotel for pets",
                "Pilaite",
                "Pet Street 5",
                true,
                true, // Is pet hotel
                3);
            hotel3.Approve();

            await _context.Hotels.AddAsync(hotel3);
            await _context.SaveChangesAsync();

            var hotel3Rooms = new List<Room>
        {
            CreateRoom(hotel3.Id, "P1", "Pet Single", 1, 1, 69.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel3.Id, "P2", "Pet Double", 2, 1, 99.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel3.Id, "P3", "Pet Suite", 3, 2, 149.99m, AccommodationType.HotelRoom, true)
        };

            // Hotel 4 - Business Center Hotel (Owner 3, Approved)
            var hotel4 = new Hotel(4, "Business Center Hotel", "Lithuania", "Vilnius");
            hotel4.Update(
                "Business Center Hotel",
                "Modern hotel for business travelers",
                "Snipiskes",
                "Business Ave 20",
                false,
                false,
                2);
            hotel4.Approve();

            await _context.Hotels.AddAsync(hotel4);
            await _context.SaveChangesAsync();

            var hotel4Rooms = new List<Room>
        {
            CreateRoom(hotel4.Id, "301", "Business Single", 1, 1, 79.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel4.Id, "302", "Business Double", 2, 1, 119.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel4.Id, "401", "Executive Office Suite", 2, 1, 189.99m, AccommodationType.HotelRoom)
        };

            // Hotel 5 - Pending approval (Owner 1)
            var hotel5 = new Hotel(2, "New Downtown Hotel", "Lithuania", "Kaunas");
            hotel5.Update(
                "New Downtown Hotel",
                "Newly opened hotel in Kaunas city center",
                "Center",
                "Liberty Ave 15",
                false,
                false,
                3);
            // Not approved - stays Pending

            await _context.Hotels.AddAsync(hotel5);
            await _context.SaveChangesAsync();

            var hotel5Rooms = new List<Room>
        {
            CreateRoom(hotel5.Id, "101", "Standard Room", 2, 1, 89.99m, AccommodationType.HotelRoom)
        };
            hotel5Rooms[0].Hide(); // Hidden until hotel approved

            // Hotel 6 - Warsaw Palace Hotel (Owner 3, Approved)
            var hotel6 = new Hotel(3, "Warsaw Palace Hotel", "Poland", "Warsaw");
            hotel6.Update(
                "Warsaw Palace Hotel",
                "Historic luxury hotel in the heart of Warsaw with royal heritage",
                "Old Town",
                "Krakowskie Przedmiescie 5",
                true,
                false,
                7);
            hotel6.Approve();

            await _context.Hotels.AddAsync(hotel6);
            await _context.SaveChangesAsync();

            var hotel6Rooms = new List<Room>
        {
            CreateRoom(hotel6.Id, "101", "Classic Room", 2, 1, 139.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel6.Id, "201", "Deluxe King Room", 2, 1, 189.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel6.Id, "301", "Royal Suite", 4, 2, 399.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel6.Id, "401", "Presidential Suite", 6, 3, 699.99m, AccommodationType.HotelRoom, true)
        };

            // Hotel 7 - Riga Art Nouveau Hotel (Owner 3, Approved)
            var hotel7 = new Hotel(3, "Riga Art Nouveau Hotel", "Latvia", "Riga");
            hotel7.Update(
                "Riga Art Nouveau Hotel",
                "Boutique hotel in beautiful Art Nouveau district of Riga",
                "Alberta iela",
                "Alberta Street 12",
                true,
                false,
                5);
            hotel7.Approve();

            await _context.Hotels.AddAsync(hotel7);
            await _context.SaveChangesAsync();

            var hotel7Rooms = new List<Room>
        {
            CreateRoom(hotel7.Id, "A101", "Cozy Single", 1, 1, 79.99m, AccommodationType.HotelRoom),
            CreateRoom(hotel7.Id, "A102", "Art Nouveau Double", 2, 1, 129.99m, AccommodationType.HotelRoom, true),
            CreateRoom(hotel7.Id, "B201", "Designer Apartment", 4, 2, 249.99m, AccommodationType.Apartment, true),
            CreateRoom(hotel7.Id, "C301", "Penthouse Suite", 6, 3, 499.99m, AccommodationType.Apartment, true)
        };

            // Hotel 8 - Tokyo Capsule Inn (Owner 3, Approved)
            var hotel8 = new Hotel(3, "Tokyo Capsule Inn", "Japan", "Tokyo");
            hotel8.Update(
                "Tokyo Capsule Inn",
                "Modern capsule hotel in the heart of Shibuya district",
                "Shibuya",
                "Shibuya Crossing 1-2-3",
                false,
                false,
                1);
            hotel8.Approve();

            await _context.Hotels.AddAsync(hotel8);
            await _context.SaveChangesAsync();

            var hotel8Rooms = new List<Room>
        {
            CreateRoom(hotel8.Id, "C-001", "Standard Capsule Pod", 1, 1, 29.99m, AccommodationType.Capsule),
            CreateRoom(hotel8.Id, "C-002", "Premium Capsule Pod", 1, 1, 39.99m, AccommodationType.Capsule),
            CreateRoom(hotel8.Id, "C-003", "Deluxe Capsule Suite", 1, 1, 49.99m, AccommodationType.Capsule)
        };

            // Hotel 9 - Berlin Budget Inn (Owner 2, REJECTED)
            var hotel9 = new Hotel(2, "Berlin Budget Inn", "Germany", "Berlin");
            hotel9.Update(
                "Berlin Budget Inn",
                "Budget accommodation near Berlin Central Station",
                "Mitte",
                "Invalidenstrasse 99",
                false,
                false,
                3);
            hotel9.Reject(); // Rejected by admin

            await _context.Hotels.AddAsync(hotel9);
            await _context.SaveChangesAsync();

            var hotel9Rooms = new List<Room>
        {
            CreateRoom(hotel9.Id, "001", "Economy Single Room", 1, 1, 45.99m, AccommodationType.HotelRoom)
        };
            hotel9Rooms[0].Hide();

            await _context.Rooms.AddRangeAsync(hotel1Rooms);
            await _context.Rooms.AddRangeAsync(hotel2Rooms);
            await _context.Rooms.AddRangeAsync(hotel3Rooms);
            await _context.Rooms.AddRangeAsync(hotel4Rooms);
            await _context.Rooms.AddRangeAsync(hotel5Rooms);
            await _context.Rooms.AddRangeAsync(hotel6Rooms);
            await _context.Rooms.AddRangeAsync(hotel7Rooms);
            await _context.Rooms.AddRangeAsync(hotel8Rooms);
            await _context.Rooms.AddRangeAsync(hotel9Rooms);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded 9 hotels with {Count} rooms",
                hotel1Rooms.Count + hotel2Rooms.Count + hotel3Rooms.Count + hotel4Rooms.Count + hotel5Rooms.Count + hotel6Rooms.Count + hotel7Rooms.Count + hotel8Rooms.Count + hotel9Rooms.Count);
        }

        private Room CreateRoom(
            int hotelId,
            string roomNumber,
            string description,
            int capacity,
            int bedrooms,
            decimal price,
            AccommodationType accommodation,
            bool petsAllowed = false)
        {
            var room = new Room(hotelId, capacity, bedrooms, price);
            room.Update(roomNumber, description, capacity, bedrooms, price, petsAllowed, accommodation);
            return room;
        }
    }
}
