using System.Text;
using AllureExporter.Client;
using AllureExporter.Models;
using JsonWriter;
using Microsoft.Extensions.Logging;
using Models;

namespace AllureExporter.Services;

public class StepService : IStepService
{
    private readonly ILogger<StepService> _logger;
    private readonly IClient _client;
    private readonly IWriteService _writeService;
        private readonly IAttachmentService _attachmentService;

    public StepService(ILogger<StepService> logger, IClient client, IWriteService writeService
        )
    {
        _logger = logger;
        _client = client;
        _writeService = writeService;
    }

    public async Task<List<Step>> ConvertSteps(int testCaseId, Guid testCaseGuid)
    {
        var steps = await _client.GetSteps(testCaseId);
        /*
        _logger.LogDebug("Found steps: {@Steps}", steps);

        _logger.LogInformation("Getting attachments for step for test case with id {TestCaseId}", testCaseId);

        foreach (var step in steps)
        {
            foreach (var stepStep in step.Steps.Where(stepStep =>
                             stepStep.Attachments != null)) {
                _logger.LogDebug("Found attachments: {@Attachments}", stepStep.Attachments);
                _logger.LogInformation("Downloading attachments");
                foreach (var attachment in stepStep.Attachments)
                {
                    _logger.LogDebug("Downloading attachment: {Name}", attachment.Name);
                    var bytes = await _client.DownloadAttachment(attachment.Id);
                    var name = await _writeService.WriteAttachment(testCaseGuid, bytes, attachment.Name);
                }
            }
        }
        */
        
        return steps.Select(allureStep =>
            {
                var attachments = new List<string>();

                foreach (var allureStepStep in allureStep.Steps.Where(allureStepStep =>
                             allureStepStep.Attachments != null))
                {
                    attachments.AddRange(allureStepStep.Attachments!.Select(a => a.Name));
                }

                var step = new Step
                {
                    Action = GetStepAction(allureStep),
                    ActionAttachments = allureStep.Attachments != null
                        ? allureStep.Attachments.Select(a => a.Name).ToList()
                        : new List<string>(),
                    ExpectedAttachments = new List<string>(),
                    TestDataAttachments = new List<string>(),
                    Expected = allureStep.ExpectedResult
                };

                step.ActionAttachments.AddRange(attachments);

                return step;
            })
            .ToList();
    }

    private static string GetStepAction(AllureStep step)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(step.Keyword))
        {
            builder.AppendLine($"<p>{step.Keyword}</p>");
        }

        builder.AppendLine($"<p>{step.Name}</p>");

        step.Steps
            .ForEach(s =>
            {
                if (!string.IsNullOrEmpty(s.Keyword))
                {
                    builder.AppendLine($"<p>{s.Keyword}</p>");
                }

                builder.AppendLine($"<p>{s.Name}</p>");
            });

        return builder.ToString();
    }
}
