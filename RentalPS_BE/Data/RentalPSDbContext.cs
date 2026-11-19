using Microsoft.EntityFrameworkCore;
using RentalPSMiniProject.Models;

namespace RentalPSMiniProject.Data
{
    public class RentalPSDBContext : DbContext
    {
        public RentalPSDBContext(DbContextOptions<RentalPSDBContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<ConsoleType> ConsoleTypes { get; set; } = null!;
        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<StickType> StickTypes { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<ConsoleUnit> ConsoleUnits { get; set; } = null!;
        public DbSet<Stick> Sticks { get; set; } = null!;
        public DbSet<Rental> Rentals { get; set; } = null!;
        public DbSet<RentalRoom> RentalRooms { get; set; } = null!;
        public DbSet<RentalStick> RentalSticks { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TABLE MAPPING
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<ConsoleType>().ToTable("ConsoleTypes");
            modelBuilder.Entity<RoomType>().ToTable("RoomTypes");
            modelBuilder.Entity<StickType>().ToTable("StickTypes");
            modelBuilder.Entity<Room>().ToTable("Rooms");
            modelBuilder.Entity<ConsoleUnit>().ToTable("ConsoleUnits");
            modelBuilder.Entity<Stick>().ToTable("Sticks");
            modelBuilder.Entity<Rental>().ToTable("Rentals");
            modelBuilder.Entity<RentalRoom>().ToTable("RentalRooms");
            modelBuilder.Entity<RentalStick>().ToTable("RentalSticks");
            modelBuilder.Entity<Payment>().ToTable("Payments");

            // COLUMN MAPPING
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Name).HasColumnName("name");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.Password).HasColumnName("password");
                entity.Property(u => u.Role).HasColumnName("role");
            });

            modelBuilder.Entity<ConsoleType>(entity =>
            {
                entity.Property(c => c.Id).HasColumnName("id");
                entity.Property(c => c.Name).HasColumnName("name");
                entity.Property(c => c.HourlyRate).HasColumnName("hourly_rate");
                entity.Property(c => c.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<RoomType>(entity =>
            {
                entity.Property(rt => rt.Id).HasColumnName("id");
                entity.Property(rt => rt.Name).HasColumnName("name");
                entity.Property(rt => rt.AdditionalHourlyRate).HasColumnName("additional_hourly_rate");
                entity.Property(rt => rt.Description).HasColumnName("description");
                entity.Property(rt => rt.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<StickType>(entity =>
            {
                entity.Property(st => st.Id).HasColumnName("id");
                entity.Property(st => st.ConsoleTypeId).HasColumnName("console_type_id");
                entity.Property(st => st.Name).HasColumnName("name");
                entity.Property(st => st.AdditionalHourlyRate).HasColumnName("additional_hourly_rate");
                entity.Property(st => st.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.RoomTypeId).HasColumnName("room_type_id");
                entity.Property(r => r.RoomNumber).HasColumnName("room_number");
                entity.Property(r => r.Description).HasColumnName("description");
                entity.Property(r => r.StatusRoom).HasColumnName("status_room");
            });

            modelBuilder.Entity<ConsoleUnit>(entity =>
            {
                entity.Property(cu => cu.Id).HasColumnName("id");
                entity.Property(cu => cu.ConsoleTypeId).HasColumnName("console_type_id");
                entity.Property(cu => cu.RoomId).HasColumnName("room_id");
                entity.Property(cu => cu.UnitCode).HasColumnName("unit_code");
                entity.Property(cu => cu.StatusConsole).HasColumnName("status_console");
            });

            modelBuilder.Entity<Stick>(entity =>
            {
                entity.Property(s => s.Id).HasColumnName("id");
                entity.Property(s => s.StickTypeId).HasColumnName("controller_type_id");
                entity.Property(s => s.StickCode).HasColumnName("controller_code");
                entity.Property(s => s.StatusStick).HasColumnName("status_stick");
            });

            modelBuilder.Entity<Rental>(entity =>
            {
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.RentalCode).HasColumnName("rental_code");
                entity.Property(r => r.UserId).HasColumnName("user_id");
                entity.Property(r => r.ConsoleUnitId).HasColumnName("console_unit_id");
                entity.Property(r => r.StartTime).HasColumnName("start_time");
                entity.Property(r => r.EndTime).HasColumnName("end_time");
                entity.Property(r => r.DurationHour).HasColumnName("duration_hours");
                entity.Property(r => r.ConsoleRate).HasColumnName("console_rate");
                entity.Property(r => r.TotalAmount).HasColumnName("total_amount");
                entity.Property(r => r.StatusRental).HasColumnName("status_rental");
            });

            modelBuilder.Entity<RentalRoom>(entity =>
            {
                entity.Property(rr => rr.Id).HasColumnName("id");
                entity.Property(rr => rr.RentalId).HasColumnName("rental_id");
                entity.Property(rr => rr.RoomId).HasColumnName("room_id");
                entity.Property(rr => rr.Name).HasColumnName("room_type_name");
                entity.Property(rr => rr.AdditionalHourlyRate).HasColumnName("additional_hourly_rate");
                entity.Property(rr => rr.SubTotal).HasColumnName("subtotal");
            });

            modelBuilder.Entity<RentalStick>(entity =>
            {
                entity.Property(rs => rs.Id).HasColumnName("id");
                entity.Property(rs => rs.RentalId).HasColumnName("rental_id");
                entity.Property(rs => rs.StickId).HasColumnName("controller_id");
                entity.Property(rs => rs.HourlyRate).HasColumnName("hourly_rate");
                entity.Property(rs => rs.SubTotal).HasColumnName("subtotal");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.RentalId).HasColumnName("rental_id");
                entity.Property(p => p.PaymentCode).HasColumnName("payment_code");
                entity.Property(p => p.Amount).HasColumnName("amount");
                entity.Property(p => p.PaymentMethod).HasColumnName("payment_method");
                entity.Property(p => p.PaymentStatus).HasColumnName("payment_status");
                entity.Property(p => p.PaidAt).HasColumnName("paid_at");
            });

            // UNIQUE INDEXES
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ConsoleType>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<RoomType>()
                .HasIndex(rt => rt.Name)
                .IsUnique();

            modelBuilder.Entity<StickType>()
                .HasIndex(st => new { st.ConsoleTypeId, st.Name })
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomNumber)
                .IsUnique();

            modelBuilder.Entity<ConsoleUnit>()
                .HasIndex(cu => cu.UnitCode)
                .IsUnique();

            modelBuilder.Entity<Stick>()
                .HasIndex(s => s.StickCode)
                .IsUnique();

            modelBuilder.Entity<Rental>()
                .HasIndex(r => r.RentalCode)
                .IsUnique();

            // Relasi one-to-one Rental - RentalRoom
            modelBuilder.Entity<RentalStick>()
                .HasIndex(rs => new { rs.RentalId, rs.StickId })
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentCode)
                .IsUnique();

            // ENUM CONVERSION
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<Role>(value, true));

            modelBuilder.Entity<Room>()
                .Property(r => r.StatusRoom)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<StatusRoom>(value, true));

            modelBuilder.Entity<ConsoleUnit>()
                .Property(cu => cu.StatusConsole)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<StatusConsole>(value, true));

            modelBuilder.Entity<Stick>()
                .Property(s => s.StatusStick)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<StatusStick>(value, true));

            modelBuilder.Entity<Rental>()
                .Property(r => r.StatusRental)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<StatusRental>(value, true));

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentMethod)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<PaymentMethod>(value, true));

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentStatus)
                .HasConversion(
                    value => value.ToString().ToLowerInvariant(),
                    value => Enum.Parse<PaymentStatus>(value, true));


            // User (1) ---- (N) Rental
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.User)
                .WithMany(u => u.Rentals)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ConsoleType (1) ---- (N) ConsoleUnit
            modelBuilder.Entity<ConsoleUnit>()
                .HasOne(cu => cu.ConsoleType)
                .WithMany()
                .HasForeignKey(cu => cu.ConsoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ConsoleType (1) ---- (N) StickType
            modelBuilder.Entity<StickType>()
                .HasOne(st => st.ConsoleType)
                .WithMany(ct => ct.StickTypes)
                .HasForeignKey(st => st.ConsoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // RoomType (1) ---- (N) Room
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Room (1) ---- (N) ConsoleUnit
            modelBuilder.Entity<ConsoleUnit>()
                .HasOne(cu => cu.Room)
                .WithMany(r => r.ConsoleUnits)
                .HasForeignKey(cu => cu.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // StickType (1) ---- (N) Stick
            modelBuilder.Entity<Stick>()
                .HasOne(s => s.StickType)
                .WithMany(st => st.Sticks)
                .HasForeignKey(s => s.StickTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ConsoleUnit (1) ---- (N) Rental
            modelBuilder.Entity<Rental>()
                .HasOne(r => r.ConsoleUnit)
                .WithMany()
                .HasForeignKey(r => r.ConsoleUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental (1) ---- (1) RentalRoom
            modelBuilder.Entity<RentalRoom>()
                .HasOne(rr => rr.Rental)
                .WithOne()
                .HasForeignKey<RentalRoom>(rr => rr.RentalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Room (1) ---- (N) RentalRoom
            modelBuilder.Entity<RentalRoom>()
                .HasOne(rr => rr.Room)
                .WithMany()
                .HasForeignKey(rr => rr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental (1) ---- (N) RentalStick
            modelBuilder.Entity<RentalStick>()
                .HasOne(rs => rs.Rental)
                .WithMany()
                .HasForeignKey(rs => rs.RentalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Stick (1) ---- (N) RentalStick
            modelBuilder.Entity<RentalStick>()
                .HasOne(rs => rs.Stick)
                .WithMany()
                .HasForeignKey(rs => rs.StickId)
                .OnDelete(DeleteBehavior.Restrict);

            // Rental (1) ---- (N) Payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Rental)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RentalId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
