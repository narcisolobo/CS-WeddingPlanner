using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeddingPlanner.Models;

namespace WeddingPlanner.Controllers {
    public class HomeController : Controller {

        private WeddingPlannerContext dbContext;

        public HomeController (WeddingPlannerContext context) {
            dbContext = context;
        }

        [HttpGet]
        [Route ("")]
        public IActionResult Index () {
            return View ();
        }

        [HttpGet]
        [Route ("register")]
        public IActionResult Register () {
            return View ("Register");
        }

        [HttpPost]
        [Route ("processregistration")]
        public IActionResult ProcessRegistration (User newUser) {
            if (ModelState.IsValid) {
                if (dbContext.Users.Any (u => u.Email == newUser.Email)) {
                    ModelState.AddModelError ("Email",
                        "Email already in use. Please log in.");
                    return View ("Register");
                }
                PasswordHasher<User> Hasher = new PasswordHasher<User> ();
                newUser.Password = Hasher.HashPassword (newUser, newUser.Password);
                dbContext.Users.Add (newUser);
                dbContext.SaveChanges ();
                User loggedUser = dbContext.Users.FirstOrDefault ((u => u.Email == newUser.Email));
                HttpContext.Session.SetInt32 ("logged", loggedUser.UserId);
                return RedirectToAction ("Dashboard");
            } else {
                return View ("Register");
            }
        }

        [HttpGet]
        [Route ("dashboard")]
        public IActionResult Dashboard () {
            int flag = CheckLogged();
            if (flag == 0) {
                return View ("Index");
            }
            User loggedUser = dbContext.Users.FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("logged"));
            PopulateBag ();
            return View ("Dashboard", loggedUser);
        }

        [HttpGet]
        [Route ("login")]
        public IActionResult Login () {
            return View ("Login");
        }

        [HttpPost]
        [Route ("processlogin")]
        public IActionResult ProcessLogin (LoginUser userSubmission) {
            if (ModelState.IsValid) {
                var userInDb = dbContext.Users.FirstOrDefault (u => u.Email == userSubmission.Email);
                if (userInDb == null) {
                    ModelState.AddModelError ("Email", "Invalid Email");
                    return View ("Login");
                }

                var hasher = new PasswordHasher<LoginUser> ();
                var result = hasher.VerifyHashedPassword (userSubmission, userInDb.Password, userSubmission.Password);
                if (result == 0) {
                    ModelState.AddModelError ("Password", "Invalid Password");
                    return View ("Login");
                }
                User loggedUser = userInDb;
                HttpContext.Session.SetInt32 ("logged", loggedUser.UserId);
                return RedirectToAction ("Dashboard");
            } else {
                return View ("Login");
            }
        }

        [HttpGet]
        [Route ("newwedding")]
        public IActionResult NewWedding () {
            int flag = CheckLogged();
            if (flag == 0) {
                return View ("Index");
            }
            return View ("NewWedding");
        }

        [HttpPost]
        [Route ("processwedding")]
        public IActionResult ProcessWedding (Wedding newWedding) {
            if (ModelState.IsValid) {
                if (newWedding.WeddingDate < DateTime.Now) {
                    TempData["alertMessage"] = "<p style='color:red;'>Date of wedding must be in the future.</p>";
                    return RedirectToAction ("NewWedding");
                }
                User loggedUser = dbContext.Users.FirstOrDefault (u => u.UserId == HttpContext.Session.GetInt32 ("logged"));
                newWedding.Creator = loggedUser;
                newWedding.UserId = loggedUser.UserId;
                dbContext.Weddings.Add (newWedding);
                dbContext.SaveChanges ();
                return RedirectToAction ("ViewWedding", new { weddingId = newWedding.WeddingId });
            }
            return View ("NewWedding");
        }

        [HttpGet]
        [Route ("viewwedding/{weddingId}")]
        public IActionResult ViewWedding (int weddingId) {
            int flag = CheckLogged();
            if (flag == 0) {
                return View ("Index");
            }
            Wedding retrievedWedding = dbContext.Weddings.FirstOrDefault (w => w.WeddingId == weddingId);
            GetWeddingGuests(weddingId);
            return View ("ViewWedding", retrievedWedding);
        }

        [HttpGet]
        [Route ("logout")]
        public IActionResult Logout () {
            HttpContext.Session.Clear ();
            return View ("Index");
        }

        [HttpGet]
        [Route ("delete/{weddingId}")]
        public IActionResult DeleteWedding (int weddingId) {
            Wedding retrievedWedding = dbContext.Weddings.FirstOrDefault (w => w.WeddingId == weddingId);
            dbContext.Weddings.Remove (retrievedWedding);
            dbContext.SaveChanges ();
            PopulateBag ();
            return View ("Dashboard");
        }

        [HttpGet]
        [Route ("RSVP/{weddingId}")]
        public IActionResult RSVPToWedding (int weddingId) {
            Wedding retrievedWedding = dbContext.Weddings.FirstOrDefault (w => w.WeddingId == weddingId);
            User loggedUser = dbContext.Users.FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("logged"));
            RSVP newRSVP = new RSVP() {
                UserId = loggedUser.UserId,
                WeddingId = retrievedWedding.WeddingId,
                User = loggedUser,
                Wedding = retrievedWedding,
            };
            dbContext.RSVPs.Add(newRSVP);
            dbContext.SaveChanges ();
            PopulateBag ();
            return RedirectToAction ("Dashboard");
        }

        [HttpGet]
        [Route ("unRSVP/{weddingId}")]
        public IActionResult UnRSVPToWedding (int weddingId) {
            Wedding retrievedWedding = dbContext.Weddings.FirstOrDefault (w => w.WeddingId == weddingId);
            User loggedUser = dbContext.Users.FirstOrDefault(u => u.UserId == HttpContext.Session.GetInt32("logged"));
            List<RSVP> retrievedRSVPs = dbContext.RSVPs
                .Where(r => r.WeddingId == retrievedWedding.WeddingId).ToList();
            RSVP retrievedRSVP = retrievedRSVPs.FirstOrDefault(r => r.UserId == loggedUser.UserId);
            dbContext.Remove(retrievedRSVP);
            dbContext.SaveChanges ();
            PopulateBag ();
            return RedirectToAction ("Dashboard");
        }

        public void PopulateBag () {
            User loggedUser = dbContext.Users.FirstOrDefault (u => u.UserId == HttpContext.Session.GetInt32 ("logged"));
            List<Wedding> weddingsWithGuestsAndUsers = dbContext.Weddings
                .Include (w => w.Guests)
                .ThenInclude (g => g.User)
                .ToList ();
            List<RSVP> usersRSVPs = dbContext.RSVPs.Where (r => r.User.Equals (loggedUser)).ToList ();
            ViewBag.LoggedUserId = HttpContext.Session.GetInt32 ("logged");
            ViewBag.WeddingsWithGuestsAndUsers = weddingsWithGuestsAndUsers;
            ViewBag.LoggedUser = loggedUser;
            ViewBag.UsersRSVPs = usersRSVPs;
        }

        public void GetWeddingGuests (int weddingId) {
            List<RSVP> weddingGuests = dbContext.RSVPs
                .Where(r => r.WeddingId == weddingId)
                .Include(r => r.User)
                .ToList();
            ViewBag.WeddingGuests = weddingGuests;
        }

        public int CheckLogged (){
            int flag = 1;
            if (HttpContext.Session.GetInt32 ("logged") == null) {
                flag = 0;
                TempData["alertMessage"] = "<p style='color:red;'>Please login or register.</p>";
            }
            return flag;
        }
    }
}