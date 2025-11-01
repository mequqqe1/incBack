using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SharpAuthDemo.Models;

// Аватар: только файл
public class AvatarUploadForm
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

// Диплом: файл + мета
public class DiplomaUploadForm
{
    [Required]
    public IFormFile File { get; set; } = default!;

    public string? Title { get; set; }
    public string? FileName { get; set; }
}