using Blog.Data;
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
[Authorize]
[SwaggerTag("Blog Crud İşlemleri")]
public class BlogsController(
    AppDbContext context,
    UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpGet("getall")]
    [ProducesResponseType<BlogPostDto[]>(StatusCodes.Status200OK)]
    [SwaggerOperation(Description = "Bütün blog yazılarını dizi halinde getirir.")]
    public IActionResult Index() =>
        Ok(context.Blogs
            .Where(b =>
                b.UserId == userManager.GetUserId(User) &&
                b.Status == BlogPost.PostStatus.Approved)
            .Adapt<BlogPostDto[]>());
    

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType<BlogPostDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Id'si girilen blog postunu getirir.")]
    public IActionResult GetBlogPostById(int id) => context.Blogs.FirstOrDefault(b =>
            b.Id == id && b.UserId == userManager.GetUserId(User) && b.Status == BlogPost.PostStatus.Approved)
        is BlogPost blog
        ? Ok(blog.Adapt<BlogPostDto>())
        : NotFound("Blog not found");

    [HttpPost("")]
    public IActionResult CreateBlog(BlogPostCreateDto newBlogPost)
    {
        var blog = newBlogPost.Adapt<BlogPost>();
        blog.UserId = userManager.GetUserId(User);
        if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
        {
            blog.Status = BlogPost.PostStatus.Approved;
        }
        else
        {
            blog.Status = BlogPost.PostStatus.Pending;
        }

        context.Blogs.Add(blog);
        context.SaveChanges();
        return CreatedAtAction(nameof(GetBlogPostById), new { id = blog.Id }, blog.Adapt<BlogPostCreateDto>());
    }

    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType<BlogPostUpdatedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Id'si girilen blog postunu günceller.")]
    public IActionResult Update(int id, BlogPostUpdatedDto postUpdatedBlogPostDto)
    {
        if (context.Blogs.AsNoTracking().FirstOrDefault(b => b.Id == id) is not BlogPost blog)
        {
            return NotFound("Blog not found");
        }

        var updatedBlog = postUpdatedBlogPostDto.Adapt<BlogPost>();
        updatedBlog.Id = id;
        blog.UserId = userManager.GetUserId(User);
        if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
        {
            blog.Status = BlogPost.PostStatus.Approved;
        }
        else
        {
            blog.Status = BlogPost.PostStatus.Pending;
        }

        updatedBlog.Modified = DateTime.UtcNow;
        context.Blogs.Update(updatedBlog);
        
        context.SaveChanges();

        return Ok("The blog has been updated. You need to wait for the approval of the blog from the moderator");
    }

    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Id'si girilen blog postunu siler." +
                                    " Bunun için kullanıcının sağlaması gereken nitelikler: Admin - Moderator veya Yazı sahibi olması gerek." )]
    public IActionResult DeleteBlog(int id)
    {
        var blog = context.Blogs
            .Include(b => b.User)
            .FirstOrDefault(b => b.Id == id);
        
        if (context.Blogs.Find(id) == null)
        {
            return NotFound("Blog not found");
        }
        var currentUserId = userManager.GetUserId(User);
        var isOwner = blog.UserId == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        var isModerator = User.IsInRole("Moderator");

        if (isOwner || isAdmin || isModerator)
        {
            context.Blogs.Remove(blog);
            context.SaveChanges();
            return Ok();
        }
        
        return NoContent();
    }
}