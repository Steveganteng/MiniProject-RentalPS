using Microsoft.EntityFrameworkCore;

namespace RentalPS_BE.Data
{
    public class RentalPSDbContext : DbContext
    {
        public RentalPSDbContext(DbContextOptions<RentalPSDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ConsoleType> ConsoleTypes { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<StickType> StickTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<ConsoleUnit> ConsoleUnits { get; set; }
        public DbSet<Stick> Sticks { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<RentalRoom> RentalRooms { get; set; }
        public DbSet<RentalStick> RentalSticks { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<ConsoleType>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<RoomTypes>()
                .HasIndex(r =>  r.Name)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomNumber)
                .IsUnique();

            modelBuilder.Entity<ConsoleUnit>()
                .HasIndex(p => p.UnitCode)
                .IsUnique();

            modelBuilder.Entity<ControllerUnit>()
                .HasIndex(c => c.ControllerCode)
                .IsUnique();

            modelBuilder.Entity<Rental>()
                .HasIndex(r => r.RentalCode)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentCode)
                .IsUnique();

            modelBuilder.Entity<ControllerType>()
                .HasIndex(c => new { c.ConsoleTypeId, c.Name })
                .IsUnique();

            modelBuilder.Entity<RentalController>()
                .HasIndex(rc => new { rc.RentalId, rc.ControllerId })
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Room>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ConsoleUnit>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ControllerUnit>()
                .Property(c => c.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Rental>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentMethod)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<ConsoleUnit>()
                .HasOne(p => p.ConsoleType)
                .WithMany(c => c.ConsoleUnits)
                .HasForeignKey(p => p.ConsoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ControllerType>()
                .HasOne(p => p.ConsoleType)
                .WithMany(c => ct.ControllerTypes)
                .HasForeignKey(c => c.ConsoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsoleUnit>()
                .HasOne(p => p.Room)
                .WithMany(r => r.ConsoleUnits)
                .HasForeignKey(p => p.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ControllerUnit>()
                .HasOne(c => c.ControllerType)
                .WithMany(ct => ct.Controllers)
                .HasForeignKey(c => c.ControllerTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rental>()
                .HasOne(r => r.User)
                .WithMany(u => u.Rentals)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rental>()
               .HasOne(r => r.PsUnit)
               .WithMany(p => p.Rentals)
               .HasForeignKey(r => r.PsUnitId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentalRoom>()
                .HasOne(rr => rr.Rental)
                .WithOne(r => r.RentalRoom)
                .HasForeignKey<RentalRoom>(rr => rr.RentalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentalRoom>()
                .HasOne(rr => rr.Room)
                .WithMany(r => r.RentalRooms)
                .HasForeignKey(rr => rr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentalController>()
                .HasOne(rc => rc.Rental)
                .WithMany(r => r.RentalControllers)
                .HasForeignKey(rc => rc.RentalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentalController>()
                .HasOne(rc => rc.Controller)
                .WithMany(c => c.RentalControllers)
                .HasForeignKey(rc => rc.ControllerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
