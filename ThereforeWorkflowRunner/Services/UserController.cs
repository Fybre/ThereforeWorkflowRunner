using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using ThereforeWorkflowRunner.Services;

namespace ThereforeWorkflowRunner;

public class UserController
{
    private readonly JobDBContext _db;
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger, JobDBContext db)
    {
        _logger = logger;   
        _db = db;
    }

    public async Task<User?> GetUserAsync(string accessKey)
    {
        return await _db.Users.Where(x => x.AuthKey == accessKey).FirstOrDefaultAsync();
    }
    
    public async Task<User?> GetUserByNameAsync(string username)
    {
        return await _db.Users.Where(x=> x.Name.ToLower() == username.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _db.Users.ToListAsync();
    }
    public async Task<Roles> GetUserRoleAsync(string accessKey)
    {
        return (await GetUserAsync(accessKey))?.Role??Roles.None;
    }

    public async Task<User> AdduserAsync(string accessKey, string username, Roles role )
    {
        var user = new User { AuthKey = accessKey, Name = username, Role = role };
        var u = await GetUserAsync(accessKey);
        if (u != null) {return u;}
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }
    public async Task<bool> DeleteUserAsync(string accessKey)
    {
        var u = await GetUserAsync(accessKey);
        if (u == null) {return false;}
        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        return true;
    }
}
