using System.Text;
using JsonWriter;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PractiTestExporter.Client;
using PractiTestExporter.Models;
using PractiTestExporter.Services;
using Constants = PractiTestExporter.Models.Constants;

namespace PractiTestExporterTests;

public class AttachmentServiceTests
{
    private ILogger<AttachmentService> _logger;
    private IClient _client;
    private IWriteService _writeService;
    private List<PractiTestAttachment> _attachments;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<AttachmentService>>();
        _client = Substitute.For<IClient>();
        _writeService = Substitute.For<IWriteService>();
        _attachments = new List<PractiTestAttachment>
        {
            new()
            {
                Id = "123",
                Type = "test",
                Attributes = new AttachmentAttributes{ Name = "Test.txt" }
            }
        };
    }

    [Test]
    public async Task DownloadAttachment_Success()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();
        var entityType = Constants.TestCaseEntityType;
        var entityId = "123";

        _client.GetAttachmentsByEntityId(entityType, entityId)
            .Returns(_attachments);
        _client.DownloadAttachmentById(Arg.Any<string>())
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Returns("Test.txt");

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        var result = await service.DownloadAttachments(entityType, entityId, guid);

        // Assert
        Assert.That(result, Is.EqualTo(new List<string> { "Test.txt" }));
    }

    [Test]
    public async Task DownloadAttachment_FailedGetAttachmentsByEntityId()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var entityType = Constants.TestCaseEntityType;
        var entityId = "123";

        _client.GetAttachmentsByEntityId(entityType, entityId)
            .Throws(new Exception("Failed to get attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(entityType, entityId, guid));

        // Assert
        await _client.DidNotReceive()
            .DownloadAttachmentById(Arg.Any<string>());
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachment_FailedDownloadAttachment()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var entityType = Constants.TestCaseEntityType;
        var entityId = "123";

        _client.GetAttachmentsByEntityId(entityType, entityId)
            .Returns(_attachments);
        _client.DownloadAttachmentById(Arg.Any<string>())
            .Throws(new Exception("Failed to download attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(entityType, entityId, guid));

        // Assert
        await _writeService.DidNotReceive()
            .WriteAttachment(Arg.Any<Guid>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Test]
    public async Task DownloadAttachment_FailedWriteAttachment()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Test");
        var guid = Guid.NewGuid();
        var entityType = Constants.TestCaseEntityType;
        var entityId = "123";

        _client.GetAttachmentsByEntityId(entityType, entityId)
            .Returns(_attachments);
        _client.DownloadAttachmentById(Arg.Any<string>())
            .Returns(bytes);

        _writeService.WriteAttachment(guid, bytes, Arg.Any<string>())
            .Throws(new Exception("Failed to write attachment"));

        var service = new AttachmentService(_logger, _client, _writeService);

        // Act
        Assert.ThrowsAsync<Exception>(async () =>
            await service.DownloadAttachments(entityType, entityId, guid));
    }
}
