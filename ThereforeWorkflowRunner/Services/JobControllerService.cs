using Microsoft.EntityFrameworkCore;
using ThereforeWorkflowRunner.Models;

namespace ThereforeWorkflowRunner.Services
{
    public class JobControllerService
    {
        private readonly JobDBContext _db;
        private readonly ILogger<JobControllerService> _logger;
        private readonly UserController _uc;

        public JobControllerService(ILogger<JobControllerService> logger, JobDBContext db, UserController uc)
        {
            _db = db;
            //_db.Database.EnsureDeleted();
            _db.Database.EnsureCreated();
            _uc = uc;
            _logger = logger;
        }

        public async void ClearDb(string accessKey)
        {
            if (await _uc.GetUserRoleAsync(accessKey) == Roles.Admin)
            {
                _db.Database.EnsureDeleted();
            }
        }

        public async Task<JobDetail> AddJobAsync(JobDetail job)
        {
            if (await _uc.GetUserRoleAsync(job.AuthKey) < Roles.None)
            {
                await _db.JobDetails.AddAsync(job);
                await _db.SaveChangesAsync();
            }
            return job;
        }

        internal async Task<JobDetail?> GetJobAsync_Internal(int id)
        {
            return await GetJobAsync("", id, true);
        }
        public async Task<JobDetail?> GetJobAsync(string authKey, int id, bool isInternal = false)
        {
            var userRole = await _uc.GetUserRoleAsync(authKey);
            var res = (isInternal || userRole == Roles.Admin) ? 
                            await _db.JobDetails.FindAsync(id) : 
                            await _db.JobDetails.Where(x => x.Id == id && x.AuthKey == authKey).FirstOrDefaultAsync();
            return res;
        }

        public async Task<List<JobDetail>> GetPendingJobsAsync()
        {
            return await _db.JobDetails.Where(x => x.NextRun < DateTime.UtcNow && x.Status == JobStatus.active).ToListAsync();
        }

        public async Task<List<JobDetail>> GetJobsAsync(string authKey, string? tenant = "", bool isInternal = false)
        {
            var userRole = await _uc.GetUserRoleAsync(authKey);
            if (isInternal || userRole == Roles.Admin)
            {
                return string.IsNullOrEmpty(tenant) ? 
                    await _db.JobDetails.ToListAsync() : 
                    await _db.JobDetails.Where(x => x.Tenant == tenant).ToListAsync();
            }
            return string.IsNullOrEmpty(tenant) ? 
                await _db.JobDetails.Where(x => x.AuthKey == authKey).ToListAsync() : 
                await _db.JobDetails.Where(x => x.Tenant == tenant && x.AuthKey == authKey).ToListAsync();
        }

        public async Task<bool> DeleteJobAsync(string authKey, int id)
        {
            if (await _uc.GetUserRoleAsync(authKey) != Roles.Admin) {return false;}
            var job = await GetJobAsync(authKey, id, true);
            if (job == null) {return false;}
            _db.Remove(job);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateJobAsync(string authKey, int id, JobDetail updatedJob)
        {
            var job = await GetJobAsync(authKey, id);
            if (job == null) { return false; }
            job.ThereforeURL = updatedJob.ThereforeURL;
            job.Status = updatedJob.Status;
            job.CronSchedule = updatedJob.CronSchedule;
            job.ScheduleIANATimeZone = updatedJob.ScheduleIANATimeZone;
            job.JobName = updatedJob.JobName;
            job.Status = updatedJob.Status;
            job.Tenant = updatedJob.Tenant;
            job.ThereforeAuth = updatedJob.ThereforeAuth;
            job.XeroWebHookKey = updatedJob.XeroWebHookKey;
            job.SetNextRun();
            _db.JobDetails.Update(job);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<DateTime?> UpdateJobNextRunAsync(int id)
        {
            var job = await GetJobAsync_Internal(id);
            if (job == null) { return null;}
            var nextRun = job.SetNextRun();
            _db.JobDetails.Update(job);
            await _db.SaveChangesAsync();
            return nextRun;
        }

        public async void TrimDatabaseLogs()
        {
            var records = _db.JobLogs.Where(x=> x.RunTime < DateTime.UtcNow.AddDays(-1));
            _db.JobLogs.RemoveRange(records);
            _logger.LogInformation($"Trimmed {records.Count()} records");
            await _db.SaveChangesAsync();
        }

        public async Task<JobStatus?> SetJobStatus(string authKey, int id, JobStatus status)
        {
            var job = await GetJobAsync(authKey, id);
            if (job == null) {return null;}
            job.Status = status;
            if (job.Status == JobStatus.active)
            {
                job.SetNextRun();
            }
            else
            {
                job.NextRun = null;
            }
            await _db.SaveChangesAsync();
            return job.Status;
        }

        internal async Task<bool> ProcessJobAsync_Internal(int id)
        {
            return await ProcessJobAsync("", id, true);
        }

        public async Task<bool> ProcessJobAsync(string authKey, int id, bool isInternal = false)
        {
            var jobResult = false;
            var job = isInternal? await GetJobAsync_Internal(id): await GetJobAsync(authKey, id);
            if (job == null) { return jobResult; }

            return await ProcessJobAsync(
                                    job.Tenant, 
                                    job.ThereforeURL, 
                                    job.ThereforeAuth, 
                                    job.CategoryNo, 
                                    job.FieldNo, 
                                    job.FieldCondition, 
                                    job.WorkflowNo);
        }

        public async Task<bool> ProcessJobAsync(JobDetail job)
        {
            if ( _db.Users.Where(x=>x.AuthKey == job.AuthKey).Count() == 0) {return false;}
            return await ProcessJobAsync(
                        job.Tenant, 
                        job.ThereforeURL, 
                        job.ThereforeAuth, 
                        job.CategoryNo, 
                        job.FieldNo, 
                        job.FieldCondition, 
                        job.WorkflowNo);
        }

        private async Task<bool> ProcessJobAsync(string tenant, string thereforeUrl, string thereforeAuth, int categoryNo, int fieldNo, string fieldCondition, int workflowNo)
        {
            var jobResult = false;
            var jobNos = await ThereforeService.GetDocumentsForCategory(tenant, thereforeUrl, thereforeAuth, categoryNo, fieldNo, fieldCondition);
            foreach(var j in jobNos)
            {
                _logger.LogInformation($"Starting workflow {workflowNo} on Tenant {tenant}, for DocNo {j}");
                var res = await ThereforeService.StartWorkflowInstance(tenant, thereforeUrl, thereforeAuth, j, workflowNo)??0;
                jobResult = res > 0;
                _db.JobLogs.Add(new JobLog{ JobId = 0, RunTime = DateTime.UtcNow, Status = $"{(res == 0?"ERROR ":"")}Workflow Instance {res} for DocId {j}"});
                await _db.SaveChangesAsync();
            }
            return jobResult;
        }

        public async Task<User?> AddUserAsync(string authKey, string username, Roles role, bool isInternal = false)
        {
            if (isInternal || await _uc.GetUserRoleAsync(authKey) == Roles.Admin)
            {
                if (await _uc.GetUserByNameAsync(username) != null) {return await _uc.GetUserByNameAsync(username);}
                return await _uc.AdduserAsync(isInternal?authKey:Guid.NewGuid().ToString(), username, role);
            }
            return null;
        }

        public async Task<List<User>?> GetUsersAsync(string authKey, bool isInternal = false)
        {
            return (isInternal || await _uc.GetUserRoleAsync(authKey) == Roles.Admin)?  await _uc.GetUsersAsync() : null;
        }

        public async Task<User?> GetUserAsync(string authKey, string userKey)
        {
            return await _uc.GetUserRoleAsync(authKey) == Roles.Admin ? 
                await _uc.GetUserAsync(userKey) : 
                null;
        }

        public async Task<bool> DeleteUserAsync(string authKey, string userKey)
        {
            return await _uc.GetUserRoleAsync(authKey) == Roles.Admin ?
                await _uc.DeleteUserAsync(userKey):
                false;
        }
    }
}
