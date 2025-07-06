using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Blog.Models.Entities;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }
    public string UserId { get; set; }
    public IdentityUser User { get; set; }
    public int BlogId { get; set; }
    public BlogPost BlogPost { get; set; }
    public bool IsReported { get; set; } = false;
    public int ReportedBy { get; set; } = 0;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; }
}