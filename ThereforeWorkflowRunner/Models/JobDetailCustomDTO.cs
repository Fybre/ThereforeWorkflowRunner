namespace ThereforeWorkflowRunner.Models
{
    public class JobDetailCustomDTO: JobDetail
    {
        private new int Id {get;}
        private new DateTime? NextRun {get;}
        private new string CronSchedule {get;}  = "";
        private new int Status {get;}
    }

}
