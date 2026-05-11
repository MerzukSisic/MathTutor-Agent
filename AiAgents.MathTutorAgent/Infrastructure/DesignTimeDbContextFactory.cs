using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiAgents.MathTutorAgent.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MathTutorDbContext>
{
    public MathTutorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MathTutorDbContext>();

        var provider = Environment.GetEnvironmentVariable("MATHTUTOR_DB_PROVIDER");
        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var sqlServerConnectionString = Environment.GetEnvironmentVariable("MATHTUTOR_SQLSERVER_CONNECTION")
                ?? "Server=localhost,1433;Database=MathTutorAgentDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true";
            optionsBuilder.UseSqlServer(sqlServerConnectionString);
        }
        else
        {
            var sqliteConnectionString = Environment.GetEnvironmentVariable("MATHTUTOR_SQLITE_CONNECTION")
                ?? "Data Source=mathtutor.db";
            optionsBuilder.UseSqlite(sqliteConnectionString);
        }

        return new MathTutorDbContext(optionsBuilder.Options);
    }
}
