using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using JobFindingSystem.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Net;
using Newtonsoft.Json.Linq;

namespace JobFindingSystem.Controllers
{
    public class AccountController : Controller
    {
        DBEntities db = new DBEntities();
        PasswordHasher ph = new PasswordHasher();
        
        // HELPER FUNCTIONS ---------------------------------------------------
        private string HashPassword(string password)
        {
            return ph.HashPassword(password);
        }

        private bool VerifyPassword(string hash, string password)
        {
            return ph.VerifyHashedPassword(hash, password) ==
                   PasswordVerificationResult.Success;
        }

        private void SignIn(string username, string role, bool isPersistent)
        {
            // 1. Identity and claims
            var iden = new ClaimsIdentity("AUTH");
            iden.AddClaim(new Claim(ClaimTypes.Name, username));
            iden.AddClaim(new Claim(ClaimTypes.Role, role));

            // 2. Remember me
            var prop = new AuthenticationProperties();
            prop.IsPersistent = isPersistent;

            // 3. Sign in
            var auth = Request.GetOwinContext().Authentication;
            auth.SignIn(prop, iden);
        }

        private void SignOut()
        {
            var auth = Request.GetOwinContext().Authentication;
            auth.SignOut();
        }

        private string GetPhotoError(HttpPostedFileBase f)
        {
            var reName = new Regex(@"\.(jpg|jpeg|png)$", RegexOptions.IgnoreCase);
            var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);

            if (f == null)
            {
                return "The Photo field is required.";
            }
            else if (f.ContentLength > 10 * 1024 * 1024)
            {
                return "Photo cannot more than 10MB.";
            }
            else if (!reName.IsMatch(f.FileName) || !reType.IsMatch(f.ContentType))
            {
                return "Only JPEG or PNG Photo is allowed.";
            }

            return null;
        }

        private string SavePhoto(HttpPostedFileBase f)
        {
            string path = Server.MapPath("~/Photo/Profile/");
            string name = Guid.NewGuid().ToString("N") + ".jpg";

            var img = new ImageFactory()
                .Load(f.InputStream)
                .Resize(new ResizeLayer(new Size(150, 150), ResizeMode.Crop))
                .Format(new JpegFormat())
                .Save(path + name);

            return name;
        }

        // LOGIN --------------------------------------------------------------
        public ActionResult Login(string ReturnURL = "")
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginVM vm, string ReturnURL = "")
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(x => x.email == vm.email && x.status == "active");

                if (user == null)
                {
                    ModelState.AddModelError("Password", "Password not matched.");

                }
                else
                {
                    if (accountNoBlocking(user))
                    {
                        //if password login success, then login (DONE)
                        if (VerifyPassword(user.password, vm.password))
                        {
                            SignIn(user.email, user.role, vm.RememberMe);

                            user.ipAddress = Request.UserHostAddress;
                            user.userAgentAddress = Request.UserAgent;
                            user.loginFailure = 0;
                            user.lastLogin = DateTime.Now;
                            // TODO: Session
                            Session["PhotoURL"] = user.image;
                            if (ReturnURL == "")
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        //if login failed, record ip, user agent, count
                        else
                        {
                            //if there is other PC, initial login failure
                            //ip address not same and user agent not same
                            if (user.ipAddress != Request.UserHostAddress || user.userAgentAddress != Request.UserAgent)
                            {
                                //if there is not same user
                                //overwrite pc information
                                user.ipAddress = Request.UserHostAddress;
                                user.userAgentAddress = Request.UserAgent;
                                user.loginFailure = 1;

                                ModelState.AddModelError("Password", "Password not matched..");

                            }
                            //if it is same pc, add the login failure count
                            else
                            {
                                //if count more than 3 times already
                                user.loginFailure = user.loginFailure + 1;
                                if (user.loginFailure >= db.Settings.SingleOrDefault(x => x.status == "active" && x.title == "loginFailureTimes").settingStringInt)
                                {
                                    user.nextLoginTime = DateTime.Now.AddMinutes(15);
                                    ModelState.AddModelError("Password", "Password not matched.15..");

                                }
                                ModelState.AddModelError("Password", "Password not matched...");
                            }

                            ModelState.AddModelError("Password", "Password not matched...");

                            db.SaveChanges();

                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Password", "Login failed, account has been block for 15 Minutes.");

                    }

                }
            }
            return View();
        }

        public bool accountNoBlocking(User u)
        {
            Setting s = db.Settings.SingleOrDefault(x => x.status == "active" && x.title == "loginFailureTimes");
            if (s != null)
            {
                //if more than 3 times
                if (u.loginFailure >= s.settingStringInt)
                {
                    //if it is same user login
                    if (u.ipAddress == Request.UserHostAddress && u.userAgentAddress == Request.UserAgent)
                    {
                        //if blocking time is over
                        if (u.nextLoginTime < DateTime.Now)
                        {
                            //if 15 minutes is over
                            //clear all the block
                            db.Users.SingleOrDefault(x => x.Id == u.Id).loginFailure = 0;
                            db.SaveChanges();
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        // LOGOUT -------------------------------------------------------------

        public ActionResult Logout()
        {
            SignOut();

            // TODO: Session
            Session.Remove("PhotoURL");

            return RedirectToAction("Index", "Home");
        }

        // REMOTE VALIDATIONS -------------------------------------------------

        public ActionResult CheckUserEmail(string email)
        {
            bool valid = !db.Users.Any(u => u.email == email);
            return Json(valid, JsonRequestBehavior.AllowGet);
        }

        // REGISTER -----------------------------------------------------------
        [Authorize(Roles = "Admin")]
        public ActionResult AddAccount()
        {
            return View();
        }

        public Boolean VerifyPhone(string phoneCode, string phone)
        {
            bool valid = false;
            int length = phone.Length;

            switch(phoneCode)
            {
                case "012":
                    if (length == 7)
                        valid = true;
                    break;
                case "016":
                    if (length == 7)
                        valid = true;
                    break;
                case "017":
                    if (length == 7)
                        valid = true;
                    break;
                case "018":
                    if (length == 7)
                        valid = true;
                    break;
                case "019":
                    if (length == 7)
                        valid = true;
                    break;
                case "011":
                    if (length == 8)
                        valid = true;
                    break;
                default:
                    break;
            }

            return valid;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public ActionResult AddAccount(AddAccountVM vm)
        {
            //1.check username
            if (ModelState.IsValidField("email") && db.Users.Any(u => u.email == vm.email.Trim()))
            {
                ModelState.AddModelError("email", "Email already exist! Re-enter the email.");
            }

            if (vm.role.ToLower().Trim() != "admin" && vm.role.ToLower().Trim() != "employee" && vm.role.ToLower().Trim() != "employer")
            {
                ModelState.AddModelError("role", "Invalid role! Select again.");
            }

            if(!VerifyPhone(vm.PhoneCode, vm.phone))
            {
                ModelState.AddModelError("phone", "Invalid phone number format.");
            }

            //2. check photo
            if (vm.Photo != null) // Photo is optional
            {
                string err = GetPhotoError(vm.Photo);
                if (err != null)
                {
                    ModelState.AddModelError("Photo", err);
                }
            }

            //3. insert (photo + member)
            if (ModelState.IsValid)
            {
                var phoneNo = vm.PhoneCode + vm.phone;
                var u = new User
                {
                    email = vm.email.Trim(),
                    userName = vm.username.Trim(),
                    phone = phoneNo,
                    password = HashPassword(vm.password),
                    role = vm.role,

                    //additional attribute
                    address = vm.address,
                    ipAddress = Request.UserHostAddress,
                    userAgentAddress = Request.UserAgent,
                    loginFailure = 0,
                    nextLoginTime = DateTime.Now,
                    lastLogin = DateTime.Now,
                    status = "active",
                    registeredDate = DateTime.Now
                };

                if (vm.Photo != null)
                {
                    u.image = SavePhoto(vm.Photo);
                }

                db.Users.Add(u);
                db.SaveChanges();
                TempData["info"] = "Account Email: " + u.email +" registered successfully.";
                TempData["color"] = "alert-success";
                return RedirectToAction("ListAccount", "Admin");
            }

            return View(vm);
        }

        [HttpPost]
        public ActionResult Register(UserRegisterVM vm)
        {
            TempData["info"] = "Account registered fail.";

            var response = Request["g-recaptcha-response"];
            string secretkey = "6Lexo4kUAAAAAJiBintgwG2Wz2MBYGSOjuBvdceL";

            var client = new WebClient();
            var result = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secretkey, response));
            var obj = JObject.Parse(result);
            var status = (bool)obj.SelectToken("success");
            var check = status ? "success" : "Please varify the reCaptcha.";
            ViewBag.Message = check;


            if (status)
            {
                //1.check username
                if (ModelState.IsValidField("email") && db.Users.Any(u => u.email == vm.email))
                {
                    ModelState.AddModelError("email", "Dulplicated {0}.");
                    TempData["info"] = "Invalid user email.";
                }

                //2. insert (member)
                if (ModelState.IsValid)
                {
                    var u = new User
                    {
                        email = vm.email,
                        password = HashPassword(vm.password),
                        role = vm.role,

                        //additional attribute
                        ipAddress = Request.UserHostAddress,
                        userAgentAddress = Request.UserAgent,
                        loginFailure = 0,
                        nextLoginTime = DateTime.Now,
                        lastLogin = DateTime.Now,
                        status = "active",
                        registeredDate = DateTime.Now
                    };
                    db.Users.Add(u);
                    db.SaveChanges();
                    TempData["info"] = "Account registered successfully, please login to your account.";
                    return RedirectToAction("Login");
                }
            }
            return PartialView("_Register", vm);
        }

        public ActionResult RegisterDetail()
        {
            return View();
        }

        // RECOVER ------------------------------------------------------------

        public ActionResult Recover()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Recover(object todo)
        {
            return View();
        }

        // CHANGE -------------------------------------------------------------
        [Authorize]
        public ActionResult Change()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Change(ChangeVM vm)
        {
            // TODO
            //1. validate current password
            var user = db.Users.Find(User.Identity.Name);
            if (user == null || !VerifyPassword(user.password, vm.current))
            {
                ModelState.AddModelError("Current", "Current Password not matched.");
            }

            //2. update password
            if (ModelState.IsValid)
            {
                string hash = HashPassword(vm.newpass);

                if (user.role == "Member")
                {
                    db.Users.Find(User.Identity.Name).password = hash;
                }
                else if (user.role == "Admin")
                {
                    db.Users.Find(User.Identity.Name).password = hash;

                }
                db.SaveChanges();
                TempData["info"] = "Password changed.";
                return RedirectToAction(null); //reload same page
            }


            return View();
        }
    }
}