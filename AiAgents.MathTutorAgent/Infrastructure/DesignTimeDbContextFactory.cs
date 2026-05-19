using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiAgents.MathTutorAgent.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MathTutorDbContext>
{
    public MathTutorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MathTutorDbContext>();

        var postgresConnectionString = Environment.GetEnvironmentVariable("MATHTUTOR_POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=mathtutor;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(postgresConnectionString);

        return new MathTutorDbContext(optionsBuilder.Options);
    }
}
