using Blog.Data;
using Blog.Models.Dto.Blog;
using Blog.Models.Dto.Comment;
using Blog.Models.Entities;
using FluentEmail.Core;
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
public class CommentController(
    AppDbContext context,
    UserManager<IdentityUser> userManager, IFluentEmail emailSender) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType<CommentDto[]>(StatusCodes.Status200OK)]
    public IActionResult Index() =>
        Ok(context.Blogs
            .Where(b =>
                b.UserId == userManager.GetUserId(User)).Adapt<CommentDto[]>());

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType<CommentDto[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCommentByBlogPostId(int id)
    {
        var userId = userManager.GetUserId(User);
        var blog = context.Blogs
            .Include(b => b.Comments)
            .FirstOrDefault(b => b.Id == id &&
                                 b.UserId == userId &&
                                 b.Status == BlogPost.PostStatus.Approved);
        if (blog == null)
        {
            return NotFound("Blog not found");
        }

        var comments = blog.Comments.Adapt<CommentDto[]>();
        return Ok(comments);
    }
    
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Comment oluşturur. Blog id'si ister. " +
                                    "Blog sahibine yorum yapıldığına dair bilgi emaili yollar.")]
    public async Task<IActionResult> CreateComment(CommentCreateDto newComment)
    {
        var blog = await context.Blogs
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == newComment.BlogId);

        if (blog is null || blog.Status != BlogPost.PostStatus.Approved)
            return NotFound("Blog not found");

        var userId = userManager.GetUserId(User);

        var comment = new Comment
        {
            BlogId = blog.Id,
            UserId = userId,
            Content = newComment.Content,
            Created = DateTime.UtcNow,
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();

        
        await emailSender
            .To(blog.User.Email)
            .Subject("Blog Yazınıza Yeni Yorum Geldi")
            .Body($@"
            Merhaba {blog.User.UserName},<br><br>
            Yazınıza yeni bir yorum yapıldı:<br><br>
            {newComment.Content}", isHtml: true)
            .SendAsync();

        return CreatedAtAction(nameof(GetCommentByBlogPostId), new { id = comment.Id },
            newComment.Adapt<CommentCreateDto>());
    }

    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Comment'i günceller. Comment Id'si ister")]
    public IActionResult UpdateComment(int id, CommentUpdateDto commentUpdateDto)
    {
        if (context.Comments.AsNoTracking().FirstOrDefault(b => b.Id == id) is not Comment comment)
        {
            return NotFound("Comment not found");
        }

        var updatedComment = commentUpdateDto.Adapt<Comment>();
        context.Comments.Update(updatedComment);
        context.SaveChanges();
        return NoContent();
    }

    [HttpPut("report/{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Id'si girilen comment reporlar.")]
    public async Task<IActionResult> ReportComment(int id)
    {
        var comment = await context.Comments.FindAsync(id);

        if (comment is null)
            return NotFound("Comment not found.");

        if (comment.IsReported)
            return Ok("Comment has already been reported.");

        comment.IsReported = true;
        comment.Modified = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok("Comment has been reported successfully.");
    }

    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(Description = "Id'si girilen yorumu siler. Admin, Moderator ve Yorum sahibi olmak gereklidir.")]
    public IActionResult DeleteComment(int id)
    {
        var commentOwner = context.Comments
            .Include(b => b.User)
            .FirstOrDefault(b => b.Id == id);
        
        var comment = context.Comments.Find(id);
        if (comment is null)
        {
            return NotFound("Comment not found.");
        }
        var currentUserId = userManager.GetUserId(User);
        var isOwner = commentOwner.UserId == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        var isModerator = User.IsInRole("Moderator");

        if (isModerator || isAdmin || isOwner)
        {
            context.Comments.Remove(comment);
            context.SaveChanges();
        }
        else
        {
            return NoContent();
        }
        
        return Ok("Comment has been deleted successfully.");
    }
}