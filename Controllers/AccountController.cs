/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PalaverCore.Data;
using PalaverCore.Models;
using PalaverCore.Models.AccountViewModels;
using PalaverCore.Services;

namespace PalaverCore.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;
    private readonly PalaverDbContext _dbContext;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IEmailSender emailSender,
        ILoggerFactory loggerFactory,
        PalaverDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = loggerFactory.CreateLogger<AccountController>();
        _dbContext = dbContext;
    }

    //
    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    //
    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            // Require the user to have a confirmed email before they can log on.
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "You must have a confirmed email to log in.");
                    return View(model);
                }
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(1, "User logged in.");
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning(2, "User account locked out.");
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    //
    // GET: /Account/Register
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    //
    // POST: /Account/Register
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (ModelState.IsValid)
        {
            var user = await _dbContext.NewUserAsync( model.Username, model.Email );
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Send an email with this link
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                //await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(3, "User created a new account with password.");
                return RedirectToLocal(returnUrl);
            }
            AddErrors(result);
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    //
    // POST: /Account/LogOff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOff()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation(4, "User logged out.");
        return Redirect(Url.Content("~/"));
    }

    //
    // POST: /Account/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    //
    // GET: /Account/ExternalLoginCallback
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return View(nameof(Login));
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        if (result.Succeeded)
        {
            _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        if (result.IsLockedOut)
        {
            return View("Lockout");
        }

        // If the email address already exists then associate the account to the existing one automatically.
        string email = (string)info.Principal.FindFirstValue(ClaimTypes.Email);
        User user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation(6, "User associated an existing account using {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            AddErrors(addLoginResult);
        }

        // If the user does not have an account, then ask the user to create an account.
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;
        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
    }

    //
    // POST: /Account/ExternalLoginConfirmation
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return View("ExternalLoginFailure");
            }
            var user = await _dbContext.NewUserAsync( model.Username, model.Email, true );
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                    return RedirectToLocal(returnUrl);
                }
            }
            AddErrors(result);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    // GET: /Account/ConfirmEmail
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return View("Error");
        }
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return View("Error");
        }
        var result = await _userManager.ConfirmEmailAsync(user, code);
        return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    //
    // GET: /Account/ForgotPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    //
    // POST: /Account/ForgotPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }

            // Send an email with this link
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
            return View("ForgotPasswordConfirmation");
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    //
    // GET: /Account/ForgotPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    //
    // GET: /Account/ResetPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string code = null)
    {
        return code == null ? View("Error") : View();
    }

    //
    // POST: /Account/ResetPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
        }
        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
        }
        AddErrors(result);
        return View();
    }

    //
    // GET: /Account/ResetPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private Task<User> GetCurrentUserAsync()
    {
        return _userManager.GetUserAsync(HttpContext.User);
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return Redirect(Url.Content("~/"));
        }
    }

    #endregion
}