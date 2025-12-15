# Hotel Booking - Hotels Service

Hotel and room inventory management microservice for the Hotel Booking System.

## Overview

The Hotels Service manages hotel listings, room inventory, and the approval workflow for new hotels. It provides search functionality and availability checking for the booking system.

## Key Responsibilities

- **Hotel Management**: Create, update, and manage hotel listings
- **Room Management**: Define room types, pricing, and availability
- **Approval Workflow**: Admin review and approval of new hotels
- **Search & Discovery**: Hotel search with filters (location, pets, price)
- **Availability**: Room availability checking for reservations

## Technology Stack

- **.NET 9** with ASP.NET Core
- **Entity Framework Core** with SQL Server
- **JWT** for authentication

## Domain Model

### Entities

**Hotel** (Aggregate Root)
- Id, OwnerId, Name, Description
- Country, City, District, AddressLine
- MainImageUrl
- PetsAllowed, IsPetHotel
- CancelFreeDaysBefore
- Approval (Pending/Approved/Rejected)
- SubmittedAt, ReviewedAt

**Room**
- Id, HotelId, RoomNumber
- Description, MainImageUrl
- Capacity, Bedrooms, PricePerNight
- Visible, PetsAllowed
- Accommodation (HotelRoom/Apartment/Studio/Suite/Villa/Cabin)

### Enums

- **ApprovalStatus**: Pending, Approved, Rejected
- **AccommodationType**: HotelRoom, Apartment, Studio, Suite, Villa, Cabin

## API Endpoints

### Public Endpoints

```
GET    /api/hotels/search          - Search hotels with filters
GET    /api/hotels/{id}            - Get hotel details
GET    /api/rooms/{id}             - Get room details
```

### Hotel Owner Endpoints

```
POST   /api/hotels                 - Submit new hotel (requires approval)
PATCH  /api/hotels/{id}            - Update own hotel
GET    /api/hotels/mine            - Get own hotels

POST   /api/rooms                  - Add room to own hotel
PATCH  /api/rooms/{id}             - Update own room
DELETE /api/rooms/{id}             - Delete own room
```

### Admin Endpoints

```
GET    /api/admin/hotels/pending   - Get hotels pending approval
POST   /api/admin/hotels/{id}/approve - Approve hotel
POST   /api/admin/hotels/{id}/reject  - Reject hotel
GET    /api/admin/hotels           - Get all hotels (any status)
```

### Internal Endpoints (Service-to-Service)

```
GET    /api/internal/hotels/{id}           - Get hotel details (requires API key)
GET    /api/internal/rooms/{id}            - Get room details
GET    /api/internal/rooms/{id}/availability - Check room availability
POST   /api/internal/rooms/validate        - Validate room exists and is bookable
```

## Search Functionality

### Query Parameters

```
GET /api/hotels/search?country=Lithuania&city=Vilnius&petsAllowed=true
  &minPrice=50&maxPrice=200&accommodation=Apartment&page=1&pageSize=10
```

Supported filters:
- Country, City
- PetsAllowed
- MinPrice, MaxPrice
- Accommodation type
- Pagination

Returns only **Approved** hotels with **Visible** rooms.

## Approval Workflow

1. **Hotel Owner** submits hotel → Status: `Pending`
2. **Admin** reviews submission
3. **Admin** approves → Status: `Approved` (visible in search)
4. **Admin** rejects → Status: `Rejected` (owner can resubmit after edits)

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings:DefaultConnection=<sql-connection-string>

# Service URLs
ServiceUrls:Users=http://localhost:8081
ServiceUrls:Hotels=http://localhost:8082
ServiceUrls:Reservations=http://localhost:8083
ServiceUrls:Payments=http://localhost:8084

# API Keys
ApiKeys:Services:UsersService=<api-key>
ApiKeys:Services:ReservationsService=<api-key>
ApiKeys:Services:PaymentsService=<api-key>

# JWT (for token validation)
Jwt:SecretKey=<base64-secret>
Jwt:Issuer=HotelBookingUsers
Jwt:Audience=HotelBookingAPI
```

## Running locally without docker

### Prerequisites
- .NET 9 SDK
- SQL Server LocalDB installed

### Standalone Run

```bash
# Navigate to Api project
cd Api

# Update connection string in appsettings.json
# Run migrations
dotnet ef database update

# Run the service
dotnet run
```

Service will be available at `http://localhost:7046/swagger/index.html`

### Docker Compose Run

See main repository's `README-DOCKER.md` for full orchestration setup.

## Database Schema

The service uses the `hotels` schema in the shared SQL Server database:

- `hotels.Hotels` - Hotel listings
- `hotels.Rooms` - Room inventory

## Availability Checking

The Reservations Service calls the Hotels Service to verify:
- Room exists and is visible
- Hotel is approved
- Room capacity meets guest count
- Room allows pets (if needed)

## Business Rules

### Hotel Submission
- Must include: Name, Country, City, CancelFreeDaysBefore
- Starts in `Pending` status
- Requires admin approval before appearing in search

### Room Management
- Can only add rooms to owned and approved hotels
- Room visibility controls search appearance
- Price must be > 0

### Cancellation Policy
- `CancelFreeDaysBefore` defines free cancellation window
- Example: 7 days = free cancellation up to 7 days before check-in
- Used by Reservations Service for cancellation logic

## Integration with Other Services

### Calls TO Other Services
- **Users Service**: Validate hotel owner exists

### Receives Calls FROM Other Services
- **Reservations Service**: Check room availability, get room details

All internal calls require `X-API-Key` header.

## Swagger Documentation

Interactive API documentation available at: `http://localhost:7046/swagger/index.html`


## Domain Events

- `HotelSubmitted` - New hotel submitted for review
- `HotelApproved` - Hotel approved by admin
- `HotelRejected` - Hotel rejected by admin
- `RoomCreated` - New room added to hotel


## Port
- **Local**: 7046
- **Docker**: 8082
- **Azure**: https://hotel-booking-hotels-api-evhhefafhhbrgrbs.northeurope-01.azurewebsites.net
