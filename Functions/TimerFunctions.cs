using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions;

public class TimerFunctions(ILogger<TimerFunctions> tracer)
{
    const string CRON_TIME_HOURLY = "0 0 * * * *";       // Every hour for debugging
    const string CRON_TIME_DAILY = "0 0 0 * * *";       // Every day at midnight

    [Function(nameof(SayHi))]
    public void SayHi([TimerTrigger(CRON_TIME_DAILY)] TimerJobRefreshInfo timerInfo)
    {
        tracer.LogInformation("Timer trigger function executed.");
    }
}

public class TimerJobRefreshInfo
{
    public TimerJobRefreshScheduleStatus? ScheduleStatus { get; set; } = null!;
    public bool IsPastDue { get; set; }
}

public class TimerJobRefreshScheduleStatus
{
    public DateTime Last { get; set; }
    public DateTime Next { get; set; }
    public DateTime LastUpdated { get; set; }
}
