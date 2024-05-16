namespace ThereforeWorkflowRunner.Models
{
    public class ThereforeExecuteSimpleQueryRequest
    {
        public int CategoryNo { get; set; }
        public string Condition { get; set; } = "*";
        public int FieldNo { get; set; }
        public int OrderByFieldNo { get; set; }
    }

}
