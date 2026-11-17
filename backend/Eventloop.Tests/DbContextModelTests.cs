using Eventloop.Core.Entities;
using Eventloop.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Eventloop.Tests;

public class DbContextModelTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ModelBuilder_ShouldConfigureUserEntityWithUniqueEmailIndex()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(User));

        entityType.Should().NotBeNull();
        var emailIndex = entityType!.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.Email)));
        emailIndex.Should().NotBeNull();
        emailIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void ModelBuilder_ShouldConfigureEventWithCheckConstraintAndRowVersion()
    {
        using var context = CreateDbContext();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(Event));

        entityType.Should().NotBeNull();

        // Verify Check Constraint in relational design-time model
        var checkConstraints = entityType!.GetCheckConstraints();
        checkConstraints.Should().Contain(c => c.Name == "CK_Events_Capacity_Positive");

        // Verify RowVersion concurrency token
        var rowVersionProp = entityType!.FindProperty(nameof(Event.RowVersion));
        rowVersionProp.Should().NotBeNull();
        rowVersionProp!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void ModelBuilder_ShouldConfigureRegistrationWithUniqueAndCompositeIndexes()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(Registration));

        entityType.Should().NotBeNull();

        // Unique index on (EventId, UserId)
        var userEventIndex = entityType!.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == nameof(Registration.EventId)) &&
            i.Properties.Any(p => p.Name == nameof(Registration.UserId)));
        userEventIndex.Should().NotBeNull();
        userEventIndex!.IsUnique.Should().BeTrue();

        // Unique index on TicketCode
        var ticketCodeIndex = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            i.Properties.Any(p => p.Name == nameof(Registration.TicketCode)));
        ticketCodeIndex.Should().NotBeNull();
        ticketCodeIndex!.IsUnique.Should().BeTrue();

        // Composite index on (EventId, Status)
        var eventStatusIndex = entityType.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == nameof(Registration.EventId)) &&
            i.Properties.Any(p => p.Name == nameof(Registration.Status)));
        eventStatusIndex.Should().NotBeNull();
    }
}
