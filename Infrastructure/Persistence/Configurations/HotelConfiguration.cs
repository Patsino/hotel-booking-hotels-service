using HotelBooking.Hotels.Domain.Hotels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Hotels.Infrastructure.Persistence.Configurations;

public sealed class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.ToTable(
            name: "Hotels",
            schema: "hotels",
            buildAction: tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    name: "CK_Hotels_CancelFreeDays_NonNegative",
                    sql: "[CancelFreeDaysBefore] >= 0");
            });

        builder.HasKey(hotel => hotel.Id);

        builder.Property(hotel => hotel.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(hotel => hotel.OwnerId)
            .HasColumnName("OwnerId")
            .IsRequired();

        builder.Property(hotel => hotel.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(hotel => hotel.Description)
            .HasColumnName("Description");

        builder.Property(hotel => hotel.MainImageUrl)
            .HasColumnName("MainImageUrl")
            .HasMaxLength(500);

        builder.Property(hotel => hotel.Country)
            .HasColumnName("Country")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(hotel => hotel.City)
            .HasColumnName("City")
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(hotel => hotel.District)
            .HasColumnName("District")
            .HasMaxLength(120);

        builder.Property(hotel => hotel.AddressLine)
            .HasColumnName("AddressLine")
            .HasMaxLength(300);

        builder.Property(hotel => hotel.PetsAllowed)
            .HasColumnName("PetsAllowed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(hotel => hotel.IsPetHotel)
            .HasColumnName("IsPetHotel")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(hotel => hotel.CancelFreeDaysBefore)
            .HasColumnName("CancelFreeDaysBefore")
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(hotel => hotel.Approval)
            .HasColumnName("Approval")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(ApprovalStatus.Pending);

        builder.Property(hotel => hotel.SubmittedAt)
            .HasColumnName("SubmittedAt")
            .IsRequired()
            .HasColumnType("datetimeoffset(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(hotel => hotel.ReviewedAt)
            .HasColumnName("ReviewedAt")
            .HasColumnType("datetimeoffset(7)");

        builder.HasIndex(hotel => new { hotel.Country, hotel.City })
            .HasDatabaseName("IX_Hotels_Country_City");

        builder.HasIndex(hotel => hotel.City)
            .HasDatabaseName("IX_Hotels_City");

        builder.HasIndex(hotel => hotel.Approval)
            .HasDatabaseName("IX_Hotels_Approval");

        builder.HasIndex(hotel => hotel.PetsAllowed)
            .HasDatabaseName("IX_Hotels_PetsAllowed");

        builder.HasIndex(hotel => hotel.IsPetHotel)
            .HasDatabaseName("IX_Hotels_IsPetHotel");

        builder.HasIndex(hotel => hotel.OwnerId)
            .HasDatabaseName("IX_Hotels_OwnerId");
    }
}
