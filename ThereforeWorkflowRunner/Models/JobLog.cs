namespace ThereforeWorkflowRunner.Models
{
    public class JobLog
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public DateTime RunTime { get; set; }
        public string Status { get; set; } = "";
    }
}
