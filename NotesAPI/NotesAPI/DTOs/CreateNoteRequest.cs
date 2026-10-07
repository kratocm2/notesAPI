using System.ComponentModel.DataAnnotations;

namespace NotesAPI.DTOs;

public class CreateNoteRequest
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = "";

    public int? CategoryId { get; set; }
}