using Quartz;

public class UpdateOddsTodayJob : IJob
{
    private readonly ILogger<UpdateOddsTodayJob> _logger;

    public UpdateOddsTodayJob(ILogger<UpdateOddsTodayJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Updating odds for today...");
        return Task.CompletedTask;
    }
}