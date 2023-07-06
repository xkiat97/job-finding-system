using JobFindingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using System.Drawing;
using System.Text.RegularExpressions;

using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Microsoft.Owin.Security;

namespace JobFindingSystem.Controllers
{
    public class AdminController : Controller
    {
        // Database context object
        DBEntities db = new DBEntities();

        PasswordHasher ph = new PasswordHasher();

        // GET: Admin
        public ActionResult Index()
        {
            var myPosts = db.Posts.Where(x =>
            x.Ratings.FirstOrDefault().ratingDate.Month == DateTime.Today.Month &&
            x.Ratings.FirstOrDefault().ratingDate.Year == DateTime.Today.Year);

            ViewBag.item1 = Math.Round((double)db.PostFees.Where(x => x.paymentDate.Value.Month == DateTime.Today.Month && x.paymentDate.Value.Year == DateTime.Today.Year).Sum(x => x.fee) +
                db.AccountFees.Where(x => x.paymentDate.Month == DateTime.Today.Month && x.paymentDate.Year == DateTime.Today.Year).Sum(x => x.fee), 2);

            ViewBag.item2 = db.PostReports.Where(x => x.reportDate.Month == DateTime.Today.Month && x.reportDate.Year == DateTime.Today.Year).Count();

            ViewBag.item3 = myPosts.Where(y => y.status == "completed").Sum(x => x.workingTotalWage) ?? 0;

            ViewBag.item4 = myPosts.Where(y => y.status == "posting" || y.status == "bided").Sum(x => x.workingTotalWage) ?? 0;

            ViewBag.item5 = db.Users.Count(x => x.registeredDate.Month == DateTime.Today.Month && x.registeredDate.Year == DateTime.Today.Year);

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListAccount()
        {
            var users = db.Users;
            return View(users);
        }

        public Boolean VerifyPhone(string phoneCode, string phone)
        {
            bool valid = false;
            int length = phone.Length;

            switch (phoneCode)
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
        public ActionResult AdminProfile()
        {
            int id = db.Users.SingleOrDefault(x => x.email == User.Identity.Name).Id;
            return RedirectToAction("UserProfile", new { id = id });
        }
        
        [Authorize(Roles = "Admin")]
        public ActionResult UserProfile(int? id)
        {
            if (id != null)
            {
                var user = db.Users.SingleOrDefault(u => u.Id == id);

                var relativePath = "~/Photo/Profile/" + user.image;
                var absolutePath = HttpContext.Server.MapPath(relativePath);
                if (!System.IO.File.Exists(absolutePath))
                {
                    user.image = "../noImage.png";
                }

                if (user != null)
                {
                    var phoneCode = "";
                    var phone = "";

                    if (user.phone != null)
                    {
                        phoneCode = user.phone.Substring(0, 3);
                        phone = user.phone.Substring(3);
                    }
                    

                    var vm = new ProfileVM
                    {
                        Id = user.Id,
                        Name = user.userName,
                        Email = user.email,
                        PhotoURL = user.image,
                        phone = phone,
                        PhoneCode = phoneCode,
                        UserAddress = user.address,
                        Role = user.role,
                        IP = user.ipAddress,
                        userAgent = user.userAgentAddress,
                        ReportCount = user.reportCount,
                        LoginFailure = user.loginFailure,
                        LastLogin = user.lastLogin,
                        Status = user.status,
                        RegisteredDate = user.registeredDate,
                    };
                    return View(vm);
                }
                else
                {
                    TempData["color"] = "alert-danger";
                    TempData["info"] = "User ID: " + id + " cannot be found!";
                    return RedirectToAction("ListAccount");
                }
            }
            else
            {
                TempData["color"] = "alert-danger";
                TempData["info"] = "User ID is empty!";
                return RedirectToAction("ListAccount");
            }       
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult UserProfile(ProfileVM vm)
        {   
            User u = db.Users.SingleOrDefault(user => user.Id == vm.Id);
            
            if(u != null)
            {
                var phoneCode = "";
                var phone = "";

                if (u.phone != null)
                {
                    phoneCode = u.phone.Substring(0, 3);
                    phone = u.phone.Substring(3);
                }

                //validation
                //if it is update self profile
                if (u.email == User.Identity.Name)
                {
                    if (!VerifyPassword(u.password, vm.Password))
                    {
                        ModelState.AddModelError("Password", "Password entered not matched with current password!");
                    }
                }

                if (vm.Photo != null) // Photo is optional
                {
                    string err = GetPhotoError(vm.Photo);
                    if (err != null)
                    {
                        ModelState.AddModelError("Photo", err);
                    }
                }

                if(User.Identity.Name == u.email)
                {
                    if (!VerifyPhone(vm.PhoneCode, vm.phone))
                    {
                        ModelState.AddModelError("phone", "Invalid phone number format.");
                    }
                }

                if (vm.Status == null || vm.Status == "")
                {
                    ModelState.AddModelError("status", "Status cannot be empty.");
                }
                else
                {
                    if (vm.Status.ToLower().Trim() != "active" && vm.Status.ToLower().Trim() != "inactive")
                    {
                        ModelState.AddModelError("status", "Invalid option! Select again.");
                    }
                }

                //update profile 
                if (ModelState.IsValid)
                {
                    if (vm.Photo != null) // Photo is optional
                    {
                        string path = Server.MapPath("~/Photo/Profile/");
                        u.image = SavePhoto(vm.Photo);
                    }

                    if (vm.NewPassword != null && vm.Password != null)
                    {
                        string hash = HashPassword(vm.NewPassword);
                        u.password = hash;
                    }

                    //change DB
                    u.userName = vm.Name == null ? null : vm.Name.Trim();
                    if (User.Identity.Name == u.email)
                    {
                        u.phone = vm.PhoneCode.Trim() + vm.phone.Trim();
                    }
                    u.address = vm.UserAddress == null ? null : vm.UserAddress.Trim();
                    u.status = vm.Status.Trim();

                    db.SaveChanges();
                    Session["PhotoURL"] = u.image;

                    TempData["info"] = "User ID: " + u.Id + " update profile successfully.";
                    TempData["color"] = "alert-success";
                    return RedirectToAction("ListAccount");
                }

                vm.UserAddress = u.address;
                vm.userAgent = u.userAgentAddress;
                vm.Status = u.status;
                vm.PhotoURL = u.image;
                vm.Email = u.email;
                vm.phone = phone;
                vm.PhoneCode = phoneCode;
                vm.Name = u.userName;
                vm.IP = u.ipAddress;
                vm.LastLogin = u.lastLogin;
                vm.RegisteredDate = u.registeredDate;
                vm.Role = u.role;
                vm.Id = u.Id;
                vm.ReportCount = u.reportCount;
                vm.LoginFailure = u.loginFailure;
            }
            TempData["color"] = "alert-danger";
            TempData["info"] = "User ID: " + u.Id + " contain invalid input. Fail to update profile!";
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListJob()
        {
            var jobs = db.JobCategories;

            ViewBag.Jobs = jobs;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListSkill()
        {
            var skills = db.Skills;

            ViewBag.skills = skills.ToList().OrderBy(x => x.JobCategory.name);

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult AddJob(AddJobCategoryVM vm)
        {
            var j = db.JobCategories.SingleOrDefault(v => v.name.Contains(vm.Name.Trim()));

            if (j != null)
            {
                ModelState.AddModelError("Name", "Job title already exist.");
                TempData["info"] = "Fail to add new job category! Job category title: \"" + vm.Name + "\" already exist with the ID: " + j.jobID;
                TempData["color"] = "alert-danger";
            }

            if (ModelState.IsValid)
            {
                var job = new JobCategory
                {
                    name = vm.Name.Trim(),
                    desc = vm.Description.Trim(),
                    status = "active"
                };

                db.JobCategories.Add(job);
                db.SaveChanges();
                TempData["info"] = "Job category title: \"" + job.name + "\" added successfully.";
                TempData["color"] = "alert-success";
                return RedirectToAction("ListJob");
            }

            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EditJob(int? id)
        {
            if (id != null)
            {
                var job = db.JobCategories.SingleOrDefault(j => j.jobID == id);
                ViewBag.skills = job.Skills;
                if (job != null)
                {
                    var vm = new EditJobCategoryVM
                    {
                        Id = job.jobID,
                        Name = job.name,
                        Description = job.desc,
                        Status = job.status,
                    };
                    return View(vm);
                }
                else
                {
                    TempData["color"] = "alert-danger";
                    TempData["info"] = "Job category ID: " + id + " cannot be found!";
                    return RedirectToAction("ListJob");
                }
            }
            else
            {
                TempData["color"] = "alert-danger";
                TempData["info"] = "Job category ID is empty!";
                return RedirectToAction("ListJob");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult EditJob(EditJobCategoryVM vm)
        {
            var j = db.JobCategories.SingleOrDefault(user => user.jobID == vm.Id);

            if (vm.Status.ToLower().Trim() != "active" && vm.Status.ToLower().Trim() != "inactive")
            {
                ModelState.AddModelError("status", "Invalid option! Select again.");
            }

            if (db.JobCategories.Any(v => v.name == vm.Name.Trim() && j.name != v.name))
            {
                ModelState.AddModelError("name", "Category title already exist! Re-enter the title.");
            }

            if (ModelState.IsValid)
            {
                if (j.name != vm.Name.Trim() || j.status != vm.Status.Trim() || j.desc != vm.Description.Trim())
                {
                    TempData["info"] = "Job category ID: " + j.jobID + " information updated successfully";
                }
                else
                {
                    TempData["color"] = "alert-info";
                    TempData["info"] = "Job category ID: " + j.jobID + " information remain the same";
                    return RedirectToAction("ListJob");
                }

                j.name = vm.Name.Trim();
                j.status = vm.Status.Trim();
                j.desc = vm.Description.Trim();
                db.SaveChanges();

                TempData["color"] = "alert-success";
                return RedirectToAction("ListJob");
            }

            vm.Name = j.name.Trim();
            vm.Description = j.desc.Trim();
            vm.Status = j.status.Trim();
            ViewBag.skills = j.Skills;

            TempData["color"] = "alert-danger";
            TempData["info"] = "Job category ID: " + j.jobID + " contain invalid input. Fail to update job category information!";
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult RemoveSkill(int skillID = 0)
        {
            Skill skill = db.Skills.SingleOrDefault(x => x.skillID == skillID && x.status == "active");
            if(skill != null)
            {
                skill.status = "inactive";
                db.SaveChanges();
                return RedirectToAction("EditJob", new { id = skill.jobCategoryID });

            }
            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult RestoreSkill(int skillID = 0)
        {
            Skill skill = db.Skills.SingleOrDefault(x => x.skillID == skillID && x.status == "inactive");
            if (skill != null)
            {
                skill.status = "active";
                db.SaveChanges();
                return RedirectToAction("EditJob", new { id = skill.jobCategoryID });

            }
            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListPost()
        {
            var posts = db.Posts.Where(x => x.status == "completed" || x.status == "absent");

            return View(posts);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EmploymentDetail(int postID = 0)
        {
            Post posts = db.Posts.SingleOrDefault(x => x.postID == postID && (x.status == "completed" || x.status == "absent"));
            if (posts != null)
            {
                ViewBag.posts = posts;
                return View();
            }

            return RedirectToAction("ListPost");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListComplaintedPost()
        {
            int numOfReport = db.Settings.SingleOrDefault(x => x.title == "adminViewReportedPost").settingStringInt.Value;
            var posts = db.Posts.Where(x => x.PostReports.Count >= numOfReport && (x.status == "posting" || x.status == "bided" || x.status == "reported"));
            ViewBag.posts = posts;
            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ComplaintedPost(int postID = 0)
        {
            Post post = db.Posts.SingleOrDefault(x => x.postID == postID &&
                                                x.PostReports.Count > 0 && 
                                                (x.status == "posting" || x.status == "bided" || x.status == "reported"));
            if (post != null)
            {
                if (post.status == "posting")
                {
                    post.status = "reported";
                }
                else
                {
                    post.status = "posting";
                    foreach (RequestedList item in post.RequestedLists)
                    {
                        db.RequestedLists.Remove(item);
                    }
                }
                db.SaveChanges();
            }
            return RedirectToAction("ListComplaintedPost");
        }

        [Authorize(Roles = "Admin")]
        public ActionResult SubscribeFee()
        {
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;
            ViewBag.rpt = db.AccountFees.Where(x => x.paymentDate.Month == month && x.paymentDate.Year == year);
            ViewBag.pkg = db.Settings.Where(x => x.type == "accountSubscribe").ToList();
            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult PostFee()
        {
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;
            ViewBag.rpt = db.PostFees.Where(x => x.paymentDate.Value.Month == month && x.paymentDate.Value.Year == year);
            ViewBag.pkg = db.Settings.Where(x => x.type == "postFee").ToList();

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult ListEmployement()
        {
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;
            ViewBag.rpt = db.Posts.Where(x => x.status == "completed" && x.Ratings.FirstOrDefault().ratingDate.Year == year && x.Ratings.FirstOrDefault().ratingDate.Month == month);
            return View();
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

        private string HashPassword(string password)
        {
            return ph.HashPassword(password);
        }

        private bool VerifyPassword(string hash, string password)
        {
            return ph.VerifyHashedPassword(hash, password) ==
                   PasswordVerificationResult.Success;
        }
    }
    
}