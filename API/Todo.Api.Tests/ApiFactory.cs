using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Todo.Infrastructure;

public class ApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // <-- ez aktiválja a Program.cs guardot

        builder.ConfigureServices(services =>
        {
            // Biztonságból kiszedjük az esetlegesen regisztrált DbContextOptions-t
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<TodoDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // In-memory SQLite (kapcsolat életben tartva a teljes tesztfutam alatt)
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<TodoDbContext>(opt =>
                opt.UseSqlite(_connection));

            // Sémát felhúzzuk
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
