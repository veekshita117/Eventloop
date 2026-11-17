using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Eventloop.Tests;

public class AzureQueueRegistrationEventPublisherTests
{
    [Fact]
    public async Task PublishTicketIssuedAsync_WhenQueueUnreachable_DoesNotThrowException()
    {
        // Arrange: Invalid unreachable connection string
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AzureWebJobsStorage", "DefaultEndpointsProtocol=http;AccountName=invalid;AccountKey=invalid;QueueEndpoint=http://127.0.0.1:9999/invalid;" }
            })
            .Build();

        var publisher = new AzureQueueRegistrationEventPublisher(
            config,
            NullLogger<AzureQueueRegistrationEventPublisher>.Instance);

        var reg = new Registration
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            Status = RegistrationStatus.Confirmed,
            TicketCode = Guid.NewGuid()
        };

        var ev = new Event
        {
            Id = reg.EventId,
            Title = "Tech Fest"
        };

        // Act & Assert: Must not throw an exception (safe failure policy)
        var act = async () => await publisher.PublishTicketIssuedAsync(reg, ev);
        await act.Should().NotThrowAsync();
    }
}
