using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Blog.Models.Entities;

public class BlogPost
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; }
    [Required]
    public string Brief { get; set; }
    [Required, MaxLength(300)]
    public string Content { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Pending;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public string UserId { get; set; }
    public IdentityUser User { get; set; }
    
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; }

    public enum PostStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
    }
}