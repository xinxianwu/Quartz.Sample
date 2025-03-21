using System.Collections.Concurrent;

namespace Quartz.Sample.JobHistory;

public class JobExecutionHistoryService
{
    private readonly ConcurrentDictionary<string, List<JobExecutionRecord>> _executionHistory = new();
    private readonly int _maxHistoryPerJob = 100;  // 每個 Job 最多保留的歷史記錄數
    
    public void RecordExecution(IJobExecutionContext context, TimeSpan duration, bool wasSuccessful, Exception exception = null)
    {
        var jobKey = context.JobDetail.Key.ToString();
        var record = new JobExecutionRecord
        {
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            TriggerName = context.Trigger.Key.Name,
            TriggerGroup = context.Trigger.Key.Group,
            FireTime = context.FireTimeUtc.LocalDateTime,
            ExecutionDuration = duration,
            WasSuccessful = wasSuccessful,
            ExceptionMessage = exception?.Message,
            StackTrace = exception?.StackTrace
        };

        _executionHistory.AddOrUpdate(
            jobKey,
            _ => [record],
            (_, list) => {
                list.Add(record);
                // 保持列表不超過最大容量
                if (list.Count > _maxHistoryPerJob)
                {
                    return list.Skip(list.Count - _maxHistoryPerJob).ToList();
                }
                return list;
            });
    }

    public List<JobExecutionRecord> GetExecutionHistory(string jobName = null, string jobGroup = null)
    {
        if (string.IsNullOrEmpty(jobName) && string.IsNullOrEmpty(jobGroup))
        {
            return _executionHistory.Values.SelectMany(x => x).OrderByDescending(x => x.FireTime).ToList();
        }
        
        if (!string.IsNullOrEmpty(jobName) && !string.IsNullOrEmpty(jobGroup))
        {
            var jobKey = new JobKey(jobName, jobGroup).ToString();
            return _executionHistory.TryGetValue(jobKey, out var history) 
                ? history.OrderByDescending(x => x.FireTime).ToList() 
                : new List<JobExecutionRecord>();
        }
        
        return _executionHistory.Values
            .SelectMany(x => x)
            .Where(r => 
                (string.IsNullOrEmpty(jobName) || r.JobName == jobName) && 
                (string.IsNullOrEmpty(jobGroup) || r.JobGroup == jobGroup))
            .OrderByDescending(x => x.FireTime)
            .ToList();
    }
    
    public void ClearHistory(string jobName = null, string jobGroup = null)
    {
        if (string.IsNullOrEmpty(jobName) && string.IsNullOrEmpty(jobGroup))
        {
            _executionHistory.Clear();
            return;
        }
        
        if (!string.IsNullOrEmpty(jobName) && !string.IsNullOrEmpty(jobGroup))
        {
            var jobKey = new JobKey(jobName, jobGroup).ToString();
            _executionHistory.TryRemove(jobKey, out _);
            return;
        }
        
        var keysToRemove = _executionHistory.Keys
            .Where(k => {
                var parts = k.Split('.');
                return (string.IsNullOrEmpty(jobName) || parts[1] == jobName) &&
                       (string.IsNullOrEmpty(jobGroup) || parts[0] == jobGroup);
            })
            .ToList();
            
        foreach (var key in keysToRemove)
        {
            _executionHistory.TryRemove(key, out _);
        }
    }
}

public class JobExecutionRecord
{
    public string JobName { get; set; }
    public string JobGroup { get; set; }
    public string TriggerName { get; set; }
    public string TriggerGroup { get; set; }
    public DateTime FireTime { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public bool WasSuccessful { get; set; }
    public string ExceptionMessage { get; set; }
    public string StackTrace { get; set; }
}