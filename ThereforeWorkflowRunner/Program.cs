using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Xml;
using ThereforeWorkflowRunner;
using ThereforeWorkflowRunner.Models;
using ThereforeWorkflowRunner.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSqlite<JobDBContext>("data source=./Data/data.db");
builder.Services.AddScoped<UserController>();
builder.Services.AddScoped<JobControllerService>();
builder.Services.AddScoped<ThereforeService>();
builder.Services.AddHostedService<BackgroundTimerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();


// ***************************
// JOB ROUTES
// ***************************
app.MapPost("/addjob", async (JobDetailDTO job, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /addjob Called");
    if (job != null )
    {
        var addedJob = await jc.AddJobAsync((JobDetail)job);
        return Results.Created<JobDetail>("", addedJob);
    }
    return Results.BadRequest();
})
.WithName("AddJob")
.WithOpenApi();

app.MapPut("/updatejob", async (int id, string authKey, JobDetailDTO job, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /updatejob Called");
    if (job != null )
    {
        var updatedJob = await jc.UpdateJobAsync(authKey, id, job);
        if (updatedJob)
        {
            return Results.Ok(updatedJob);
        };
    }
    return Results.BadRequest();
})
.WithName("UpdateJob")
.WithOpenApi();

app.MapGet("/getjob", async (int id, string authKey, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /getjob Called");
    var res = await jc.GetJobAsync(authKey, id);
    if (res != null)
    {
        return Results.Ok<JobDetail>(res);
    }
    return Results.NotFound(id);
})
    .WithName("GetJob")
    .WithOpenApi();

app.MapGet("/getjobs", async (string? tenant, string authKey, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE: /getjobs called");
    return await jc.GetJobsAsync(authKey, tenant);
})
    .WithName("GetJobs")
    .WithOpenApi();

app.MapPost("/setjobstatus", async (int id, JobStatus status, string authKey, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /setjobstatus Called");
    var res = await jc.SetJobStatus(authKey, id, status);
    return (res != null)?  Results.Ok(res):Results.BadRequest();
})
    .WithName("SetJobStatus")
    .WithOpenApi();

app.MapPost("/runjob/{id}", async (int id, string authKey, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /runjob Called");
    var res = await jc.ProcessJobAsync(authKey, id);
    return res? Results.Ok(res):Results.BadRequest(res);
})
.WithName("RunJob")
.WithOpenApi();

app.MapPost("/runcustomjob", async ([FromBody] JobDetailCustomDTO jobDetails,
                                    JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /runcustomjob Called");
    var res = await jc.ProcessJobAsync(jobDetails);
    return res? Results.Ok(res):Results.BadRequest(res);
})
.WithName("RunCustomJob")
.WithOpenApi();


app.MapDelete("/deletejob", async (int id, string authKey, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /deletejob Called");
    return await jc.DeleteJobAsync(authKey, id);
})
    .WithName("DeleteJob")
    .WithOpenApi();


// ***************************
// USER ROUTES
// ***************************
app.MapPost("/adduser", async (string username, Roles role, string authKey, JobControllerService jc) => {
    app.Logger.LogInformation("ROUTE /adduser Called");
    var user = await jc.AddUserAsync(authKey, username, role);
    if (user == null)
    {
        return Results.BadRequest();
    }
    return Results.Ok(user);
})
    .WithDescription("AddUser")
    .WithOpenApi();

app.MapGet("/getusers", async (string authKey, JobControllerService jc) => {
    app.Logger.LogInformation("ROUTE /getusers Called");
    return await jc.GetUsersAsync(authKey);
})
    .WithDescription("GetUsers")
    .WithOpenApi();

app.MapGet("/getuser", async(string authKey, string userKey, JobControllerService jc) => {
    app.Logger.LogInformation("ROUTE /getuser Called");
    return await jc.GetUserAsync(authKey, userKey);
})
    .WithDescription("GetUser")
    .WithOpenApi();

app.MapDelete("/deleteuser", async (string authKey, string userKey, JobControllerService jc) =>{
    app.Logger.LogInformation("ROUTE /deleteuser Called");
    return await jc.DeleteUserAsync(authKey, userKey);
})
    .WithDescription("DeleteUser")
    .WithOpenApi();

// ***************************
// WEBHOOK ROUTES
// ***************************

app.MapPost("/xerowebhook", async (HttpRequest request, JobControllerService jc) =>
{
    app.Logger.LogInformation("ROUTE /xerowebhook Called");
    string XeroSignature = request.Headers["x-xero-signature"].ToString();
    string strBody = string.Empty;
    using (var reader = new StreamReader(request.Body))
    {
        strBody = await reader.ReadToEndAsync();
    }

    var jobs = await jc.GetJobsAsync("","",true);

    JobDetail? jobDetail = null; 
    foreach(var job in jobs)
    {
         if (WebhookController.GetHMAC256Hash(job.XeroWebHookKey, strBody) == XeroSignature)
         {
            jobDetail = job;
            break;
         }
    }

    if (jobDetail == null) {return Results.Unauthorized();}
    var webhookEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<XeroWebhookEvent>(strBody);
    
    app.Logger.LogInformation($"Matched to {jobDetail.JobName}");

    //Intent to receive check
    if (webhookEvent == null) {return Results.BadRequest();}

    if (webhookEvent.events.Count > 0)
    {
        await jc.ProcessJobAsync_Internal(jobDetail.Id);
    }

    return Results.Ok();
})
    .WithName("XeroWebhook")
    .WithOpenApi();


// ***************************
// WEB PAGE ROUTES
// ***************************

app.MapGet("/addjobweb", () => {
    return Results.File("addjobweb.html", contentType: "text/html");
}).ExcludeFromDescription();

app.MapGet("/adduserweb", () => {
    return Results.File("adduserweb.html", contentType: "text/html");
}).ExcludeFromDescription();

app.MapGet("/getusersweb", () => {
    return Results.File("getusersweb.html", contentType: "text/html");
}).ExcludeFromDescription();

app.MapGet("/getjobsweb", () => {
    return Results.File("getjobsweb.html", contentType: "text/html");
}).ExcludeFromDescription();

app.MapFallbackToFile("index.html"); // fallback to the index file if any other requests

app.Run();

