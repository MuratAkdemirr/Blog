using System.ComponentModel.DataAnnotations;

namespace Blog.Models.Dto.Comment;

public class CommentCreateDto
{
    [Required]
    public int BlogId { get; set; }
    [Required]
    public string Content { get; set; }
}
    