
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SQLitePCL;
using ThereforeWorkflowRunner.Models;

namespace ThereforeWorkflowRunner.Services
{
    public class BackgroundTimerService : IHostedService
    {
        private readonly System.Timers.Timer _timer;
        private const int FIRST_RUN_INTERVAL = 5000;
        private const int INTERVAL = 30000;
        private const int MAINTENANCE_INTERVAL = 240000;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackgroundTimerService> _logger;
        private DateTime _NextMaintenanceRun = DateTime.UtcNow.AddMilliseconds(MAINTENANCE_INTERVAL);
        private CancellationToken _CancellationToken = new CancellationToken();

        public BackgroundTimerService(ILogger<BackgroundTimerService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _timer = new System.Timers.Timer(FIRST_RUN_INTERVAL);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false;
            _scopeFactory = serviceScopeFactory;
            _logger = logger;
            StartupTasks();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {           
            _timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }

        private async void StartupTasks()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var jobController = scope.ServiceProvider.GetService<JobControllerService>();
                if (jobController != null) 
                {
                    // Add default users if users.json file exists
                    if (File.Exists("Data/users.json"))
                    {
                        _logger.LogInformation("users.json file found");
                        var usersTxt = File.ReadAllText("Data/users.json");
                        var usersToAdd = JsonConvert.DeserializeObject<List<User>>(usersTxt);
                        if (usersToAdd != null)
                        {
                            foreach(var u in usersToAdd)
                            {
                                _logger.LogInformation($"Adding user: {u.Name}");
                                await jobController.AddUserAsync(u.AuthKey, u.Name,u.Role, true);
                            }
                        }
                    }
                    var userRes = await jobController.GetUsersAsync("", true);
                    if ((userRes?.Count??0) == 0) {
                        _logger.LogWarning("No users found");
                        var authKey = Guid.NewGuid().ToString();
                        var user = await jobController.AddUserAsync(authKey, "AutoAdmin", Roles.Admin, true);
                        _logger.LogWarning($"Added User: {user.Id}, {user.Name}, {user.AuthKey}");
                    }
                }
            }
        }

        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            await StopAsync(_CancellationToken);
            using (var scope = _scopeFactory.CreateScope())
            {
                var jobController = scope.ServiceProvider.GetService<JobControllerService>();
                if (jobController != null) 
                {
                    var jobs = await jobController.GetPendingJobsAsync();
                    _logger.LogInformation($"{jobs.Count} jobs to process");
                    foreach (var job in jobs)
                    {
                        // process the job
                        _logger.LogInformation($"Job: {job.JobName} ({job.Id})");
                        
                        var res = await jobController.ProcessJobAsync_Internal(job.Id);
                        _logger.LogInformation($"ProcessJob returned {res}");
                        var NextRun = await jobController.UpdateJobNextRunAsync(job.Id);
                        _logger.LogInformation($"Job {job.JobName} ({job.Id} - next run at {NextRun}");
                    }

                    if (DateTime.UtcNow > _NextMaintenanceRun)
                    {
                        _logger.LogInformation("Performing cleanup jobs");
                        jobController.TrimDatabaseLogs();
                        _NextMaintenanceRun = DateTime.UtcNow.AddMilliseconds(MAINTENANCE_INTERVAL);
                    }
                }    
            }
            _timer.Interval = INTERVAL;
            await StartAsync(_CancellationToken);
        }
    }
}
