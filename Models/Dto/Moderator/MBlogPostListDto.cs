using Blog.Models.Entities;

namespace Blog.Models.Dto;

public class MBlogPostListDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int UserId { get; set; }
    public BlogPost.PostStatus Status { get; set; }
    
}