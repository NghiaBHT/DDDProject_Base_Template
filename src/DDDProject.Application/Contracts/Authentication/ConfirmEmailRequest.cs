using System.ComponentModel.DataAnnotations;

namespace DDDProject.Application.Contracts.Authentication;

public record ConfirmEmailRequest(
    [Required] string UserId,
    [Required] string Code
); 