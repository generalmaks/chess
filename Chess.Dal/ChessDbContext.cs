using Chess.Dal.Chess;
using Chess.Dal.Player;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal;

public class ChessDbContext(DbContextOptions<ChessDbContext> options) : DbContext(options)
{
    public DbSet<PlayerDao> Players => Set<PlayerDao>();
    public DbSet<GameDao> Games => Set<GameDao>();
    public DbSet<MoveDao> Moves => Set<MoveDao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDao>(entity =>
        {
            entity.Property(p => p.Username).HasMaxLength(32);
            entity.Property(p => p.PasswordHash).HasMaxLength(256);
            entity.Property(p => p.EloRating).HasDefaultValue(PlayerDao.DefaultEloRating);
            entity.HasIndex(p => p.Username).IsUnique();
        });

        modelBuilder.Entity<GameDao>(entity =>
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

        modelBuilder.Entity<MoveDao>(entity =>
        {
            entity.Property(m => m.Team).HasConversion<string>().HasMaxLength(8);
            entity.Property(m => m.San).HasMaxLength(8).HasDefaultValue("");

            entity.HasIndex(m => new { m.GameId, m.Ply }).IsUnique();
        });
    }
}
