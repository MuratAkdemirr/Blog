using Blog.Data;
using Blog.Models.Dto;
using Blog.Models.Dto.Blog;
using Blog.Models.Entities;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;


namespace Blog.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Moderator,Admin")]
public class ModeratorController(
    AppDbContext context,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) : ControllerBase
{
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Blogların statülerini günceller. " +
                                    "Pending = 0, Approved = 1, Rejected = 2,")]
    public IActionResult StatusControl(int id, BlogPostStatusDto statusDto)
    {
        var blog = context.Blogs.FirstOrDefault(b => b.Id == id);
        if (blog == null)
        {
            return NotFound("Blog bulunamadı");
        }

        blog.Status = statusDto.Status;
        blog.Modified = DateTime.UtcNow;
        context.Blogs.Update(blog);
        context.SaveChanges();

        return Ok(blog);
    }

    [HttpGet("pending")]
    [ProducesResponseType<MBlogPostListDto[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Pending durumunda olan postları listeler.")]
    public IActionResult GetPendingBlogPosts()
    {
        var pendingBlogs = context.Blogs
            .AsNoTracking()
            .Where(b => b.Status == BlogPost.PostStatus.Pending)
            .ToList();

        if (pendingBlogs.Count == 0)
        {
            return NotFound("No pending blog posts");
        }

        return Ok(pendingBlogs.Adapt<MBlogPostListDto[]>());
    }
}