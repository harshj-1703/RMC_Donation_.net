﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using RMC_Donation.Models;
using System.Linq;
using System.Configuration;
using System.Net;
using System.IO;

namespace RMC_Donation.Controllers
{
    public class UsersController : Controller
    {
        rmcdonateEntities entity = new rmcdonateEntities();
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel credentials)
        {
            bool userExists = entity.users.Any(x => x.email == credentials.email && x.password == credentials.password);
            user u = entity.users.FirstOrDefault(x => x.email == credentials.email && x.password == credentials.password);
            if (userExists)
            {
                if (u.status == 1)
                {
                    FormsAuthentication.SetAuthCookie(u.fullname, false);
                    var userProfile = new HttpCookie("userProfile", u.profilephoto);
                    Response.Cookies.Add(userProfile);
                    Session["user_id"] = u.id;
                    Session["user_role"] = "User";
                    u.lastlogin = DateTime.Now;
                    entity.SaveChanges();
                    return RedirectToAction("Index", "Home");
                }
                else if (u.status == 2)
                {
                    FormsAuthentication.SetAuthCookie(u.fullname, false);
                    var userProfile = new HttpCookie("userProfile", u.profilephoto);
                    Response.Cookies.Add(userProfile);
                    Session["user_id"] = u.id;
                    Session["user_role"] = "Admin";
                    u.lastlogin = DateTime.Now;
                    entity.SaveChanges();
                    return RedirectToAction("Index", "Home");
                }

                else
                {
                    ModelState.AddModelError("", "You were removed by Admin");
                    return View();
                }
            }
            ModelState.AddModelError("", "Username or password is wrong");
            return View();
        }

        [HttpPost]
        public ActionResult Signup(user userinfo, HttpPostedFileBase profilePhotoFile)
        {
            bool userExists = entity.users.Any(x => x.email == userinfo.email || x.mobile_no == userinfo.mobile_no);
            if (!userExists)
            {
                if (profilePhotoFile != null && profilePhotoFile.ContentLength > 0)
                {
                    string originalFileName = Path.GetFileName(profilePhotoFile.FileName);
                    string fileExtension = Path.GetExtension(originalFileName);

                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                    if (!allowedExtensions.Contains(fileExtension.ToLower()))
                    {
                        ModelState.AddModelError("", "Invalid file format. Only image files (jpg, jpeg, png, gif) are allowed.");
                        return View();
                    }

                    string uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;

                    string targetDirectory1 = Server.MapPath("~/Uploads/ProfilePhotos/");
                    string targetDirectory = "/Uploads/ProfilePhotos/";

                    if (!Directory.Exists(targetDirectory1))
                    {
                        Directory.CreateDirectory(targetDirectory1);
                    }

                    //string filePath = Path.Combine(targetDirectory, uniqueFileName);
                    string filePath = targetDirectory + uniqueFileName;
                    profilePhotoFile.SaveAs(targetDirectory1 + uniqueFileName);
                    userinfo.profilephoto = filePath;
                }
                else
                {
                    string targetDirectory1 = Server.MapPath("~/Uploads/ProfilePhotos/NullImages/");
                    if (!Directory.Exists(targetDirectory1))
                    {
                        Directory.CreateDirectory(targetDirectory1);
                    }
                    userinfo.profilephoto = "/Uploads/ProfilePhotos/NullImages/status1.png";
                }
                userinfo.createdat = DateTime.Now;
                userinfo.updatedat = DateTime.Now;
                userinfo.lastlogin = DateTime.Now;
                userinfo.status = 1;
                entity.users.Add(userinfo);
                entity.SaveChanges();
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "User Exists");
            return View();
        }

        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [Authorize]
        public ActionResult UserDetails(int userId)
        {
            var userDb = new rmcdonateEntities();
            var userDetails = userDb.users.SingleOrDefault(user => user.id == userId);

            if (userDetails == null)
            {
                return HttpNotFound();
            }

            return View(userDetails);
        }

        [Authorize]
        [HttpGet]
        public ActionResult EditUserProfile(int userId)
        {
            using (var db = new rmcdonateEntities())
            {
                var user = db.users.Find(userId);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                if (user.id != (int)Session["user_id"])
                {
                    return RedirectToAction("Login");
                }
                return View(user);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUserProfile(user users, HttpPostedFileBase profileImage)
        {
            using (var db = new rmcdonateEntities())
            {
                var oldUser = db.users.Find(users.id);
                if (oldUser == null || oldUser.id != (int)Session["user_id"])
                {
                    return RedirectToAction("Index", "Home");
                }

                users.profilephoto = oldUser.profilephoto;

                ModelState.Remove("email");
                ModelState.Remove("password");

                if (!ModelState.IsValid)
                {
                    List<string> modelErrors = new List<string>();
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            modelErrors.Add(error.ErrorMessage);
                        }
                    }
                    ViewBag.ModelErrors = modelErrors;
                    return View(users);
                }

                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                if (profileImage != null && profileImage.ContentLength > 0)
                {
                    string originalFileName = Path.GetFileName(profileImage.FileName);
                    string fileExtension = Path.GetExtension(originalFileName);
                    if (!allowedExtensions.Contains(fileExtension.ToLower()))
                    {
                        ModelState.AddModelError("", "Invalid file format. Only image files (jpg, jpeg, png, gif) are allowed.");
                        return View(users);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;
                    string targetDirectory = "/Uploads/ProfilePhotos/";
                    string filePath = targetDirectory + uniqueFileName;
                    string targetDirectory1 = Server.MapPath("~/Uploads/ProfilePhotos/");
                    profileImage.SaveAs(targetDirectory1 + uniqueFileName);
                    oldUser.profilephoto = filePath;

                    var userProfile = new HttpCookie("userProfile", oldUser.profilephoto);
                    Response.Cookies.Add(userProfile);
                }

                oldUser.fullname = users.fullname;
                oldUser.profession = users.profession;
                oldUser.dob = users.dob;
                oldUser.address = users.address;
                oldUser.mobile_no = users.mobile_no;
                oldUser.updatedat = DateTime.Now;

                db.SaveChanges();
                FormsAuthentication.SetAuthCookie(users.fullname, false);
                return RedirectToAction("Index", "Home");
            }
        }
    }
}