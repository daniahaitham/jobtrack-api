using JobTrack.Api.Data;
using JobTrack.Api.Dtos;
using JobTrack.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTrack.Api.Controllers;

/// <summary>CRUD = Create, Read, Update, Delete. Every query is limited to the logged-in user's own rows.</summary>
[ApiController]
[Authorize]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ApplicationsController(AppDbContext db)
    {
        _db = db;
    }

    // The user id comes from the token, never from the request body, so nobody can act as someone else.
    private int CurrentUserId => int.Parse(User.FindFirst("uid")!.Value);

    /// <summary>List all my applications, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ApplicationResponse>>> GetAll()
    {
        var items = await _db.Applications
            .Where(a => a.UserId == CurrentUserId)
            .OrderByDescending(a => a.AppliedOn)
            .ToListAsync();

        return Ok(items.Select(ToResponse).ToList());
    }

    /// <summary>Get one application by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var item = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
        return item is null ? NotFound() : Ok(ToResponse(item));
    }

    /// <summary>Add a new application.</summary>
    [HttpPost]
    public async Task<ActionResult<ApplicationResponse>> Create(ApplicationRequest request)
    {
        var now = DateTime.UtcNow;
        var item = new JobApplication
        {
            UserId = CurrentUserId,
            Company = request.Company.Trim(),
            Role = request.Role.Trim(),
            Link = request.Link,
            Status = request.Status,
            AppliedOn = request.AppliedOn ?? now,
            FollowUpOn = request.FollowUpOn,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Applications.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }

    /// <summary>Change an existing application (send all fields).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApplicationResponse>> Update(int id, ApplicationRequest request)
    {
        var item = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
        if (item is null)
        {
            return NotFound();
        }

        item.Company = request.Company.Trim();
        item.Role = request.Role.Trim();
        item.Link = request.Link;
        item.Status = request.Status;
        item.AppliedOn = request.AppliedOn ?? item.AppliedOn;
        item.FollowUpOn = request.FollowUpOn;
        item.Notes = request.Notes;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(item));
    }

    /// <summary>Delete an application.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id && a.UserId == CurrentUserId);
        if (item is null)
        {
            return NotFound();
        }

        _db.Applications.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ApplicationResponse ToResponse(JobApplication a) =>
        new(a.Id, a.Company, a.Role, a.Link, a.Status, a.AppliedOn, a.FollowUpOn, a.Notes, a.CreatedAt, a.UpdatedAt);
}
