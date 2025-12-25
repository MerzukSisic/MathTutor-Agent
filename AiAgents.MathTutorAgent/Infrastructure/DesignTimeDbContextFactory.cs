using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiAgents.MathTutorAgent.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MathTutorDbContext>
{
    public MathTutorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MathTutorDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MathTutorAgentDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new MathTutorDbContext(optionsBuilder.Options);
    }
}