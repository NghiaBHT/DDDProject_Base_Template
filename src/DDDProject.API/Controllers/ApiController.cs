using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DDDProject.API.Controllers;

/// <summary>
/// Base controller for API endpoints.
/// Provides access to MediatR.
/// </summary>
[ApiController]
[Route("api/[controller]")] // Standard route template
public abstract class ApiController : ControllerBase
{
    protected readonly ISender Sender; // Use ISender for sending requests
    protected readonly IPublisher Publisher; // Optional: Use IPublisher for publishing notifications

    protected ApiController(ISender sender, IPublisher publisher)
    {
        Sender = sender;
        Publisher = publisher;
    }

    // Optional: Add common helper methods here if needed
    // e.g., protected IActionResult HandleResult<T>(Result<T> result) { ... }
} 