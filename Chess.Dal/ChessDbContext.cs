using Chess.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal;

public class ChessDbContext(DbContextOptions<ChessDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<GameEntity> Games => Set<GameEntity>();
    public DbSet<MoveEntity> Moves => Set<MoveEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.Property(p => p.Username).HasMaxLength(32);
            entity.Property(p => p.PasswordHash).HasMaxLength(256);
            entity.Property(p => p.EloRating).HasDefaultValue(PlayerEntity.DefaultEloRating);
            entity.HasIndex(p => p.Username).IsUnique();
        });

        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.Property(g => g.Result).HasConversion<string>().HasMaxLength(32);

            entity.HasMany(g => g.Moves)
                .WithOne(m => m.Game)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.WhitePlayer)
                .WithMany(p => p.GamesAsWhite)
                .HasForeignKey(g => g.WhitePlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.BlackPlayer)
                .WithMany(p => p.GamesAsBlack)
                .HasForeignKey(g => g.BlackPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MoveEntity>(entity =>
        {
            entity.Property(m => m.Team).HasConversion<string>().HasMaxLength(8);

            entity.HasIndex(m => new { m.GameId, m.Ply }).IsUnique();
        });
    }
}
