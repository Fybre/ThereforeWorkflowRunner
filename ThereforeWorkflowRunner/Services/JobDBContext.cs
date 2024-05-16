using Microsoft.EntityFrameworkCore;
using ThereforeWorkflowRunner.Models;

namespace ThereforeWorkflowRunner.Services
{
    public class JobDBContext : DbContext
    {
        public DbSet<JobDetail> JobDetails { get; set; }
        public DbSet<JobLog> JobLogs { get; set; }
        public DbSet<User> Users { get; set; }

        public JobDBContext(DbContextOptions options) : base(options)
        { }
    }
}
