using NLog.Extensions.Logging;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.AddNLog();

// 註冊 JobExecutionHistoryService 作為單例服務
builder.Services.AddSingleton<JobExecutionHistoryService>();
// 註冊 JobExecutionHistoryListener 作為單例服務
builder.Services.AddSingleton<JobExecutionHistoryListener>();

builder.Services.AddQuartz(configurator =>
{
    var jobKey = new JobKey("UpdateOddsTodayJob");
    configurator.AddJob<UpdateOddsTodayJob>(jobConfigurator => { jobConfigurator.WithIdentity(jobKey); });
    configurator.AddTrigger(triggerConfigurator =>
        // interval every 5 sec
        triggerConfigurator
            .ForJob(jobKey)
            .WithIdentity("UpdateOddsTodayJobTrigger")
            .WithCronSchedule("0/5 * * * * ?")
    );
    
    // 在 Quartz 中註冊我們的監聽器
    configurator.AddJobListener<JobExecutionHistoryListener>();
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// 添加 JobInfoService 以提供 Job 資訊
builder.Services.AddTransient<JobInfoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 啟用靜態文件服務，提供 HTML 界面
app.UseStaticFiles();

// 添加 API 端點以獲取 Job 資訊
app.MapGet("/api/jobs", async (JobInfoService jobInfoService) =>
{
    return await jobInfoService.GetAllJobsInfoAsync();
})
.WithName("GetJobsInfo")
.WithOpenApi();

// 新增 API 端點以獲取 Job 執行歷史
app.MapGet("/api/jobs/history", (JobExecutionHistoryService historyService, string jobName = null, string jobGroup = null) =>
{
    try
    {
        var history = historyService.GetExecutionHistory(jobName, jobGroup);
        return Results.Ok(history);
    }
    catch (Exception ex)
    {
        return Results.Problem($"讀取執行歷史失敗: {ex.Message}");
    }
})
.WithName("GetJobExecutionHistory")
.WithOpenApi();

// 新增 API 端點以清除 Job 執行歷史
app.MapDelete("/api/jobs/history", (JobExecutionHistoryService historyService, string jobName = null, string jobGroup = null) =>
{
    try
    {
        historyService.ClearHistory(jobName, jobGroup);
        return Results.Ok(new { message = "歷史記錄已清除" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"清除執行歷史失敗: {ex.Message}");
    }
})
.WithName("ClearJobExecutionHistory")
.WithOpenApi();

// 原有的 WeatherForecast 端點
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

// 將根路徑重定向到儀表板頁面
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}