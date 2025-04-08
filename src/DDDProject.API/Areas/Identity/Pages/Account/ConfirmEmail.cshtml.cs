// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using DDDProject.Domain.Entities; // Assuming ApplicationUser is here
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace DDDProject.API.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Typically confirmation doesn't require login
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index"); // Or show an error page
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"Unable to load user with ID '{userId}'.");
                StatusMessage = "Error: Unable to find user.";
                return Page();
            }

            try
            {
                // Decode the code first
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, $"Error decoding email confirmation code for user ID '{userId}'.");
                StatusMessage = "Error: Invalid confirmation code format.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                _logger.LogInformation($"User with ID '{userId}' confirmed their email successfully.");
                StatusMessage = "Thank you for confirming your email.";
            }
            else
            {
                _logger.LogError($"Error confirming email for user with ID '{userId}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                StatusMessage = "Error confirming your email.";
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    // Optionally add more specific error messages to StatusMessage
                }
            }

            return Page();
        }
    }
} 