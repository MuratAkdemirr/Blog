using Blog.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Blog.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
[SwaggerTag("Admin.")]
public class AdminController(
    AppDbContext context,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) : ControllerBase
{
    
    [HttpPost("assign-role-by-email")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Rol Atama",
        Description = "Kullanıcının emailini kullanarak rol ataması gerçekleştirir. Roller Admin ve Moderator'dür.")]
    public async Task<IActionResult> AssignRoleByEmail([FromQuery] string email, [FromQuery] string role)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            return BadRequest("Email and role are required.");

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found.");

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.IsInRoleAsync(user, role))
            return BadRequest("User already has this role.");

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return StatusCode(500, "Failed to assign role.");

        await userManager.UpdateSecurityStampAsync(user);

        return Ok(new { msg = $"Role '{role}' has been assigned to user '{user.Email}'." });
    }
}