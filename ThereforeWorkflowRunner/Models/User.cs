namespace ThereforeWorkflowRunner;

public enum Roles{Admin, User, None};

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AuthKey { get; set; } = "";
    public Roles Role { get; set; } = Roles.User;
}
