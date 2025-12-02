using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialHotels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hotels");

            migrationBuilder.CreateTable(
                name: "Hotels",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    District = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PetsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPetHotel = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CancelFreeDaysBefore = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    Approval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hotels", x => x.Id);
                    table.CheckConstraint("CK_Hotels_CancelFreeDays_NonNegative", "[CancelFreeDaysBefore] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                schema: "hotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelId = table.Column<int>(type: "int", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Bedrooms = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PricePerNight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MainImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PetsAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Accommodation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "HotelRoom"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.CheckConstraint("CK_Rooms_Bedrooms_Positive", "[Bedrooms] >= 1");
                    table.CheckConstraint("CK_Rooms_Capacity_Positive", "[Capacity] >= 1");
                    table.CheckConstraint("CK_Rooms_Price_NonNegative", "[PricePerNight] >= 0");
                    table.ForeignKey(
                        name: "FK_Rooms_HotelId_Hotels_Id",
                        column: x => x.HotelId,
                        principalSchema: "hotels",
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Approval",
                schema: "hotels",
                table: "Hotels",
                column: "Approval");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_City",
                schema: "hotels",
                table: "Hotels",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Country_City",
                schema: "hotels",
                table: "Hotels",
                columns: new[] { "Country", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_IsPetHotel",
                schema: "hotels",
                table: "Hotels",
                column: "IsPetHotel");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_OwnerId",
                schema: "hotels",
                table: "Hotels",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_PetsAllowed",
                schema: "hotels",
                table: "Hotels",
                column: "PetsAllowed");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Accommodation",
                schema: "hotels",
                table: "Rooms",
                column: "Accommodation");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Bedrooms",
                schema: "hotels",
                table: "Rooms",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Capacity",
                schema: "hotels",
                table: "Rooms",
                column: "Capacity");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Hotel_RoomNumber",
                schema: "hotels",
                table: "Rooms",
                columns: new[] { "HotelId", "RoomNumber" },
                unique: true,
                filter: "[RoomNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HotelId",
                schema: "hotels",
                table: "Rooms",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PetsAllowed",
                schema: "hotels",
                table: "Rooms",
                column: "PetsAllowed");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PricePerNight",
                schema: "hotels",
                table: "Rooms",
                column: "PricePerNight");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Visible",
                schema: "hotels",
                table: "Rooms",
                column: "Visible");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rooms",
                schema: "hotels");

            migrationBuilder.DropTable(
                name: "Hotels",
                schema: "hotels");
        }
    }
}
