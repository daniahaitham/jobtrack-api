using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace JobTrack.Tests;

/// <summary>
/// Starts the real API in memory for tests, with its own throw-away database file
/// so tests never touch your real data.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jobtrack-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            SqliteConnection.ClearAllPools(); // release the file so it can be deleted
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Not important: it's a temp file.
        }
    }
}
