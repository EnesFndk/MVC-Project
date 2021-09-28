using Coop.Data;
using Coop.Helpers;
using Coop.Models;
using Coop.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Coop.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;


        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration config, ITokenService tokenService)
        {
            _logger = logger;
            _context = context;
            _config = config;
            _tokenService = tokenService;
        }

        [Authorize]
        public IActionResult Index2()
        {
            string token = HttpContext.Session.GetString("Token");

            if (token == null)
            {
                return (RedirectToAction("Login"));
            }

            if (!_tokenService.ValidateToken(_config["Jwt:Key"].ToString(),
                _config["Jwt:Issuer"].ToString(), token))
            {
                return (RedirectToAction("Login"));
            }
            return View();
        }

        public IActionResult Index()
        {
           var List = _context.FileContexts.ToList();

            return View(List);
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile text)
        {
            var kb = text.Length / 1024;
            if (kb > 500)
            {
                ViewBag.Message = "Dosya 500 KB den büyük";
                var List = _context.FileContexts.ToList();

                return View(List);
            }

            string fileContents;
            using (var stream = text.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                fileContents = await reader.ReadToEndAsync();

            }

            var mailadress = HttpContext.User.Claims.FirstOrDefault().Value;

            var user = _context.Users.Where(c => c.Email == mailadress).FirstOrDefault();

            _context.Add(new FileContext() { text = fileContents, date = DateTime.Now, UserId = user.Id, UserName = user.FirstName + " " +user.LastName});
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(UserForRegisterDto registerDto)
        {
            var user = _context.Users.Where(c => c.Email == registerDto.Email).FirstOrDefault();
            if (user != null)
            {
                return BadRequest("Kullanıcı bulunamadı");
            }
            byte[] passwordHash, passwordSalt;
            HashingHelper.CreatePasswordHash(registerDto.Password, out passwordHash, out passwordSalt);
            User usernew = new User()
            {
                Email = registerDto.Email,
                Status = true,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            _context.Users.Add(usernew);
            _context.SaveChanges();
            return View("Login");

        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(l => l.Email == userForLogin.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Email", "Email Yanlış");
                    return View();
                }
                var passwordCheck = HashingHelper.VerifyPasswordHash(userForLogin.Password, user.PasswordHash, user.PasswordSalt);

                if (!passwordCheck)
                {
                    ModelState.AddModelError("Password", "Geçersiz Şifre");

                    return View();
                }
                var generatedToken = _tokenService.BuildToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(), userForLogin);
                if (generatedToken != null)
                {
                    HttpContext.Session.SetString("Token", generatedToken);
                    return RedirectToAction("Index");
                }
                else
                {
                    return (RedirectToAction("Error"));
                }                
            }

            ModelState.AddModelError("", "Model Valid değil");

            return View();

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
