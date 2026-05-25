using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EngiFlow.Api.Models;

/// <summary>
/// Represents a multipart form request payload for uploading an attachment.
/// </summary>
public class UploadAttachmentRequest
{
    /// <summary>
    /// Gets or sets the binary file stream to be uploaded.
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;
}
