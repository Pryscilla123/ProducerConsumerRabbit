using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Producer.Api.Models;

namespace Producer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthController(
            SignInManager<IdentityUser> signInManager, 
            UserManager<IdentityUser> userManager
            )
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("registrar-user")]
        public async Task<ActionResult> RegistrarUsuario(UserViewModel userViewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new IdentityUser
            {
                UserName = userViewModel.Email,
                Email = userViewModel.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, userViewModel.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);

                return Ok("Usuário registrado com sucesso!");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("login-user")]
        public async Task<ActionResult> LoginUsuario(LoginUserViewModel loginUserViewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _signInManager.PasswordSignInAsync(loginUserViewModel.Email, loginUserViewModel.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return Ok("Usuário logado com sucesso!");
            }
            else
            {
                return Unauthorized("Falha ao realizar o login.");
            }
        }
    }
}
