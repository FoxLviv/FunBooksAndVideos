namespace FunBooksAndVideos.Infrastructure.Seeding;

/// <summary>Loads the demo catalog and customers. Safe to call more than once.</summary>
public interface IDemoDataSeeder
{
    void Seed();
}
