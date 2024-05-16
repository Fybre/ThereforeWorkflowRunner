using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Cronos;

namespace ThereforeWorkflowRunner.Models
{
    public enum JobStatus {test, active, inactive};

    
    public class JobDetail
    {
        private string _CronSchedule = "";

        private const string DEFAULT_IANA_TIMEZONE = "Australia/Sydney";
        public int Id { get; set; }
        public DateTime? NextRun { get; set; }
        public string JobName { get; set; } = "";
        public string CronSchedule { get {return _CronSchedule;} set {_CronSchedule = value; SetNextRun();} }
        public string ScheduleIANATimeZone { get; set; } = DEFAULT_IANA_TIMEZONE;
        public string Tenant { get; set; } = "";
        public string ThereforeURL { get; set; } = "";
        public string ThereforeAuth { get; set; } = "";
        public int WorkflowNo {get; set;}
        public int CategoryNo {get;set;}
        public int FieldNo {get; set;}
        public string FieldCondition {get; set;} = "";
        public string XeroWebHookKey {get; set;} = "";
        public JobStatus Status { get; set; } = JobStatus.active;
        public string AuthKey {get;set;} = "";
        
        public DateTime? SetNextRun()
        {
            NextRun = null;
            try 
            {
                CronExpression expression = CronExpression.Parse(CronSchedule);
                TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById(ScheduleIANATimeZone);
                NextRun = expression.GetNextOccurrence(DateTime.UtcNow, tzi);
            } catch {}
            return NextRun;     
        }
    }

}
