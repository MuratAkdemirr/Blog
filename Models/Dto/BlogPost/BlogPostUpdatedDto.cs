namespace Blog.Models.Dto.Blog;

public class BlogPostUpdatedDto
{
    public string Title {get;set;}
    public string Brief { get; set; }
    public string Content {get;set;}
    public Entities.BlogPost.PostStatus Status { get; set; }
    public DateTime Modified { get; set; }
    
}