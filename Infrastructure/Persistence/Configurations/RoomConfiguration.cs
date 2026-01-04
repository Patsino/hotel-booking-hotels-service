using HotelBooking.Hotels.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Hotels.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable(
            name: "Rooms",
            schema: "hotels",
            buildAction: tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    name: "CK_Rooms_Capacity_Positive",
                    sql: "[Capacity] >= 1");

                tableBuilder.HasCheckConstraint(
                    name: "CK_Rooms_Bedrooms_Positive",
                    sql: "[Bedrooms] >= 1");

                tableBuilder.HasCheckConstraint(
                    name: "CK_Rooms_Price_NonNegative",
                    sql: "[PricePerNight] >= 0");
            });

        builder.HasKey(room => room.Id);

        builder.Property(room => room.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(room => room.HotelId)
            .HasColumnName("HotelId")
            .IsRequired();

		builder.HasOne<Hotel>()
			.WithMany(h => h.Rooms)
			.HasForeignKey(nameof(Room.HotelId))
			.HasConstraintName("FK_Rooms_HotelId_Hotels_Id")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(room => room.RoomNumber)
            .HasColumnName("RoomNumber")
            .HasMaxLength(50);

        builder.Property(room => room.Description)
            .HasColumnName("Description");

        builder.Property(room => room.Capacity)
            .HasColumnName("Capacity")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(room => room.Bedrooms)
            .HasColumnName("Bedrooms")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(room => room.PricePerNight)
            .HasColumnName("PricePerNight")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(room => room.MainImageUrl)
            .HasColumnName("MainImageUrl")
            .HasMaxLength(500);

        builder.Property(room => room.Visible)
            .HasColumnName("Visible")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(room => room.PetsAllowed)
            .HasColumnName("PetsAllowed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(room => room.Accommodation)
            .HasColumnName("Accommodation")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(AccommodationType.HotelRoom);

        builder.Property(room => room.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired()
            .HasColumnType("datetimeoffset(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(room => room.HotelId)
            .HasDatabaseName("IX_Rooms_HotelId");

        builder.HasIndex(room => new { room.HotelId, room.RoomNumber })
            .HasDatabaseName("IX_Rooms_Hotel_RoomNumber")
            .IsUnique();

        builder.HasIndex(room => room.Capacity)
            .HasDatabaseName("IX_Rooms_Capacity");

        builder.HasIndex(room => room.Bedrooms)
            .HasDatabaseName("IX_Rooms_Bedrooms");

        builder.HasIndex(room => room.PricePerNight)
            .HasDatabaseName("IX_Rooms_PricePerNight");

        builder.HasIndex(room => room.Accommodation)
            .HasDatabaseName("IX_Rooms_Accommodation");

        builder.HasIndex(room => room.PetsAllowed)
            .HasDatabaseName("IX_Rooms_PetsAllowed");

        builder.HasIndex(room => room.Visible)
            .HasDatabaseName("IX_Rooms_Visible");
    }
}
