using Microsoft.EntityFrameworkCore;
using CampusLoop.Data;

namespace CampusLoop.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static CampusLoopDbContext CreateInMemoryDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<CampusLoopDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new CampusLoopDbContext(options);
    }
}
