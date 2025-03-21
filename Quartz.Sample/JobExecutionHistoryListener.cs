using Quartz;

public class JobExecutionHistoryListener : IJobListener
{
    private readonly JobExecutionHistoryService _historyService;
    
    public JobExecutionHistoryListener(JobExecutionHistoryService historyService)
    {
        _historyService = historyService;
    }
    
    public string Name => "JobExecutionHistoryListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        // 在執行開始時不需要特別記錄，因為我們需要等待執行完成才能計算耗時
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        // Job 被取消執行的情況
        _historyService.RecordExecution(context, TimeSpan.Zero, false, new Exception("Job execution was vetoed"));
        return Task.CompletedTask;
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
    {
        // Job 已執行完畢，記錄執行時間和結果
        var duration = context.JobRunTime;
        var wasSuccessful = jobException == null;
        
        _historyService.RecordExecution(context, duration, wasSuccessful, jobException?.InnerException);
        return Task.CompletedTask;
    }
}
