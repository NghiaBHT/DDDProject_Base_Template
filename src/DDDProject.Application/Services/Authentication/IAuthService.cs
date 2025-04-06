using DDDProject.Application.Contracts.Authentication;
using DDDProject.Application.Contracts.Common;
using System.Threading.Tasks;

namespace DDDProject.Application.Services.Authentication;

public interface IAuthService
{
    // Return a result object indicating success/failure and errors
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<Guid>> RegisterAsync(RegisterRequest request); // Return UserId or custom result
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request);
    // Add methods for ForgotPassword, ResetPassword etc. as needed
} 