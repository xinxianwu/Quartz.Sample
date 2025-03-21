using Quartz.Impl.Matchers;

namespace Quartz.Sample.JobHistory;

public class JobInfoService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public JobInfoService(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task<List<JobInfo>> GetAllJobsInfoAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        
        var result = new List<JobInfo>();
        
        foreach (var jobKey in jobKeys)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            var jobDetail = await scheduler.GetJobDetail(jobKey);
            
            foreach (var trigger in triggers)
            {
                var nextFireTime = trigger.GetNextFireTimeUtc();
                var previousFireTime = trigger.GetPreviousFireTimeUtc();
                
                var jobInfo = new JobInfo
                {
                    JobName = jobKey.Name,
                    JobGroup = jobKey.Group,
                    JobDescription = jobDetail?.Description,
                    TriggerName = trigger.Key.Name,
                    TriggerGroup = trigger.Key.Group,
                    TriggerDescription = trigger.Description,
                    NextFireTime = nextFireTime?.LocalDateTime,
                    PreviousFireTime = previousFireTime?.LocalDateTime,
                    TriggerState = await scheduler.GetTriggerState(trigger.Key),
                };

                // 添加 Cron 表達式（如果適用）
                if (trigger is ICronTrigger cronTrigger)
                {
                    jobInfo.CronExpression = cronTrigger.CronExpressionString;
                    jobInfo.Schedule = cronTrigger.CronExpressionString;
                }
                else if (trigger is ISimpleTrigger simpleTrigger)
                {
                    jobInfo.Schedule = $"每 {simpleTrigger.RepeatInterval.TotalSeconds} 秒";
                }

                result.Add(jobInfo);
            }
        }
        
        return result;
    }
}

public class JobInfo
{
    public string JobName { get; set; }
    public string JobGroup { get; set; }
    public string JobDescription { get; set; }
    public string TriggerName { get; set; }
    public string TriggerGroup { get; set; }
    public string TriggerDescription { get; set; }
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public TriggerState TriggerState { get; set; }
    public string CronExpression { get; set; }
    public string Schedule { get; set; }
}