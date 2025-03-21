using Quartz;

public class UpdateOddsTodayJob : IJob
{
    private readonly ILogger<UpdateOddsTodayJob> _logger;
    private static readonly Random _random = new Random();

    public UpdateOddsTodayJob(ILogger<UpdateOddsTodayJob> logger)
    {
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Updating odds for today...");

        try
        {
            // 隨機生成執行時間，介於 100ms 到 2000ms 之間
            int executionTime = _random.Next(100, 2000);
            
            // 模擬工作執行
            await Task.Delay(executionTime);

            // 偶爾模擬執行失敗（約 10% 的機率）
            if (_random.Next(1, 11) == 1)
            {
                throw new Exception($"隨機模擬的執行失敗，執行時間為 {executionTime}ms");
            }
            
            _logger.LogInformation("Successfully updated odds for today. Execution time: {ExecutionTime}ms", executionTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating odds for today");
            throw; // 重新拋出異常，使 Quartz 能夠捕獲到失敗狀態
        }
    }
}