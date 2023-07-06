using JobFindingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobFindingSystem.Controllers;
using ImageProcessor;
using ImageProcessor.Imaging;
using System.Drawing;
using ImageProcessor.Imaging.Formats;
using System.IO;

using Microsoft.AspNet.Identity;

using System.Text.RegularExpressions;


namespace JobFindingSystem.Controllers
{
    public class EmployeeController : Controller
    {
        DBEntities db = new DBEntities();
        PasswordHasher ph = new PasswordHasher();

        // GET: Employee
        [Authorize(Roles = "Employee")]
        public ActionResult Index()
        {
            //check whether he she is completed profile
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }
            var temp = db.Ratings.Where(x => x.User.email == User.Identity.Name &&
            x.ratingDate.Month == DateTime.Today.Month &&
            x.ratingDate.Year == DateTime.Today.Year).Sum(x => x.Post.workingTotalWage);
            if (temp != null)
            {
                ViewBag.item1 = temp;
            }
            else
            {
                ViewBag.item1 = 0.00;

            }


            ViewBag.item2 = db.RequestedLists.Where(x => x.User.email == User.Identity.Name && x.status == "success" && x.Post.status == "working").Count();

            ViewBag.item3 = db.AccountFees.Where(x => x.User.email == User.Identity.Name).Sum(x => x.fee);

            ViewBag.item4 = (db.Employees.SingleOrDefault(x => x.email == User.Identity.Name).expiredOn.Value - DateTime.Now).TotalDays.ToString("0");

            ViewBag.item5 = db.Posts.ToList().Where(x => x.status != "saved" && x.postedDate.Value.Date == DateTime.Today.Date && (x.status == "posting" || x.status == "bided")).Count();

            var myJobCategory = db.JobCategories.Where(x => x.Skills.Any(y => y.User.email == User.Identity.Name));
            var suggestedPost = db.Posts.Where(x => (x.status == "posting" || x.status == "bided") && myJobCategory.Any(y => y.name == x.JobCategory.name)).ToList();

            ViewBag.sPosts = suggestedPost;
            ViewBag.u = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            return View();
        }

        //handle profile register
        [Authorize(Roles = "Employee")]
        public ActionResult AddProfile()
        {
            if (db.Employees.Any(x => x.email == User.Identity.Name))
            {
                return RedirectToAction("EmployeeProfile");
            }
            EmployeeProfile vm = new EmployeeProfile
            {
                DOB = DateTime.Now.AddYears(-20)
            };
            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public ActionResult AddProfile(EmployeeProfile vm)
        {
            if (db.Employees.Any(x => x.email == User.Identity.Name))
            {
                return RedirectToAction("EmployeeProfile");
            }

            if (!VerifyPhone(vm.PhoneCode, vm.phone))
            {
                ModelState.AddModelError("phone", "Invalid phone number format.");
            }

            if (vm.gender != 'M' && vm.gender != 'F')
            {
                ModelState.AddModelError("gender", "Invalid gender selected.");
            }

            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
                user.userName = vm.name;
                user.phone = vm.PhoneCode + vm.phone;
                user.address = vm.address;
                if (vm.Photo != null)
                    user.image = SavePhoto(vm.Photo);

                var employee = new Employee
                {
                    email = User.Identity.Name,
                    nric = vm.nric1 + vm.nric2 + vm.nric3,
                    dateOfBirth = vm.DOB,
                    gender = vm.gender.ToString(),
                    intro = vm.intro,

                    //additional
                    expiredOn = DateTime.Now
                };
                if (vm.resume != null)
                    employee.resumeURL = SaveResume(vm.resume);
                db.Employees.Add(employee);
                db.SaveChanges();
                Session["PhotoURL"] = user.image;
                TempData["color"] = "alert-success";
                TempData["info"] = "Your profile has been updated successfully";
                return RedirectToAction("AddSkill");
            }
            return View(vm);
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

        [Authorize(Roles = "Employee")]
        public ActionResult UpdateProfile()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            var user = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
            var employee = db.Employees.SingleOrDefault(x => x.email == user.email);

            var phoneCode = "";
            var phone = "";

            if (user.phone != null)
            {
                phoneCode = user.phone.Substring(0, 3);
                phone = user.phone.Substring(3);
            }

            var nric1 = employee.nric.Substring(0,6);
            var nric2 = employee.nric.Substring(6,2);
            var nric3 = employee.nric.Substring(8,4);

            EmployeeProfile vm = new EmployeeProfile
            {
                photoURL = user.image ?? "upload.png",
                name = user.userName,
                nric1 = nric1,
                nric2 = nric2,
                nric3 = nric3,
                intro = employee.intro,
                phone = phone,
                PhoneCode = phoneCode,
                address = user.address,
                DOB = (DateTime)employee.dateOfBirth,
                gender = employee.gender[0],
                resumeURL = employee.resumeURL
            };

            return View(vm);
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

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public ActionResult UpdateProfile(EmployeeProfile vm)
        {
            var user = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            var phoneCode = "";
            var phone = "";

            if (user.phone != null)
            {
                phoneCode = user.phone.Substring(0, 3);
                phone = user.phone.Substring(3);
            }

            if (vm.Photo != null) // Photo is optional
            {
                string err = GetPhotoError(vm.Photo);
                if (err != null)
                {
                    ModelState.AddModelError("Photo", err);
                }
            }

            if (!VerifyPhone(vm.PhoneCode, vm.phone))
            {
                ModelState.AddModelError("phone", "Invalid phone number format.");
            }

            if(vm.gender != 'M' && vm.gender != 'F')
            {
                ModelState.AddModelError("gender", "Invalid gender selected.");
            }

            if (!VerifyPassword(user.password, vm.Password))
            {
                ModelState.AddModelError("Password", "Password entered not matched with current password!");
            }

            if (ModelState.IsValid)
            {
                user.userName = vm.name;
                user.phone = vm.PhoneCode.Trim() + vm.phone.Trim();
                user.address = vm.address;
                if (vm.Photo != null)
                    user.image = SavePhoto(vm.Photo);

                if (vm.NewPassword != null && vm.Password != null)
                {
                    string hash = HashPassword(vm.NewPassword);
                    user.password = hash;
                }

                var employee = db.Employees.SingleOrDefault(x => x.email == user.email);

                employee.email = User.Identity.Name;
                employee.nric = vm.nric1 + vm.nric2 + vm.nric3;
                employee.dateOfBirth = vm.DOB;
                employee.gender = vm.gender.ToString();
                employee.intro = vm.intro;
                if (vm.resume != null)
                    employee.resumeURL = SaveResume(vm.resume);

                db.SaveChanges();
                Session["PhotoURL"] = user.image;


                TempData["color"] = "alert-success";
                TempData["info"] = "Your profile has been updated successfully";
                return RedirectToAction("EmployeeProfile");
            }


            vm.photoURL = user.image;

            return View(vm);
        }

        [Authorize(Roles = "Employee")]
        public ActionResult AddSkill()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public ActionResult AddSkill(EmployeeSkill vm)
        {
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            var jc = db.JobCategories.SingleOrDefault(j => j.jobID.ToString() == vm.jobCategory && j.status == "active");

            if (jc == null)
            {
                ModelState.AddModelError("jobCategory", "Invalid option! Select again.");
            }

            if (vm.proficiency != "Beginner" && vm.proficiency != "Intermediate" && vm.proficiency != "Advance")
            {
                ModelState.AddModelError("proficiency", "Invalid option! Select again.");
            }
            
            if (db.Skills.Any(s => s.name == vm.name.Trim() && s.User.email == User.Identity.Name))
            {
                ModelState.AddModelError("name", "Title already exist! Re-enter the title.");
            }

            if (ModelState.IsValid)
            {
                var skill = new Skill
                {
                    name = vm.name,
                    desc = vm.desc,
                    proficiency = vm.proficiency,
                    jobCategoryID = int.Parse(vm.jobCategory),
                    employeeID = db.Users.SingleOrDefault(x => x.email == User.Identity.Name).Id,

                    status = "active",
                    modifiedDate = DateTime.Now
                };
                db.Skills.Add(skill);
                db.SaveChanges();

                TempData["color"] = "alert-success";
                TempData["info"] = "New Skill: " + vm.name + " registered successfully.";
                return RedirectToAction("EmployeeProfile");

            }
            return View(vm);
        }

        [Authorize(Roles = "Employee")]
        public ActionResult RemoveSkill(int skillID = 0)
        {
            Skill skill = db.Skills.SingleOrDefault(x => x.skillID == skillID && x.status == "active" && x.User.email == User.Identity.Name);
            if (skill != null)
            {
                skill.status = "inactive";
                db.SaveChanges();
                return RedirectToAction("EmployeeProfile");

            }
            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult RestoreSkill(int skillID = 0)
        {
            Skill skill = db.Skills.SingleOrDefault(x => x.skillID == skillID && x.status == "inactive" && x.User.email == User.Identity.Name);
            if (skill != null)
            {
                skill.status = "active";
                db.SaveChanges();
                return RedirectToAction("EmployeeProfile");

            }
            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult UpdateSkill(int skillID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            if (skillID == 0)
            {
                return RedirectToAction("EmployeeProfile");
            }
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            var skill = db.Skills.SingleOrDefault(x => x.skillID == skillID);
            if (skill != null)
            {
                EmployeeSkill vm = new EmployeeSkill
                {
                    ID = skill.skillID,
                    name = skill.name,
                    desc = skill.desc,
                    jobCategory = skill.jobCategoryID.ToString(),
                    proficiency = skill.proficiency
                };
                return View(vm);
            }
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public ActionResult UpdateSkill(EmployeeSkill vm)
        {
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            var jc = db.JobCategories.SingleOrDefault(j => j.jobID.ToString() == vm.jobCategory && j.status == "active");

            if(jc == null)
            {
                ModelState.AddModelError("jobCategory", "Invalid option! Select again.");
            }

            if(vm.proficiency != "Beginner" && vm.proficiency != "Intermediate" && vm.proficiency != "Advance")
            {
                ModelState.AddModelError("proficiency", "Invalid option! Select again.");
            }

            var skills = db.Skills.SingleOrDefault(s => s.skillID == vm.ID);

            if (db.Skills.Any(s => s.name == vm.name.Trim() && skills.name != s.name && s.User.email == User.Identity.Name))
            {
                ModelState.AddModelError("name", "Title already exist! Re-enter the title.");
            }

            if (ModelState.IsValid)
            {
                var skill = db.Skills.SingleOrDefault(x => x.skillID == vm.ID);

                if (skill != null)
                {
                    skill.name = vm.name;
                    skill.desc = vm.desc;
                    skill.proficiency = vm.proficiency;
                    skill.jobCategoryID = int.Parse(vm.jobCategory);
                    skill.employeeID = db.Users.SingleOrDefault(
                        x => x.email == User.Identity.Name).Id;
                    skill.modifiedDate = DateTime.Today;

                    db.SaveChanges();

                    TempData["color"] = "alert-success";
                    TempData["info"] = "Skill updated successfully.";
                    return RedirectToAction("EmployeeProfile");
                }
                TempData["color"] = "alert-danger";
                TempData["info"] = "Skill data not found.";
            }
            else
            {
                TempData["info"] = "Invalid Input.";

            }
            return View(vm);
        }

        [Authorize(Roles = "Employee")]
        public ActionResult AccountPayment()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            ViewBag.packages = db.Settings
                .Where(x => x.type == "accountSubscribe"
                && x.status == "active").ToList().OrderBy(y => y.settingStringInt);
            ViewBag.e = db.Employees.SingleOrDefault(x => x.email == User.Identity.Name);

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public ActionResult AccountPayment(EmployeeAccountFeeVM vm)
        {
            //get current user
            User u = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
            Employee e = db.Employees.SingleOrDefault(x => x.email == User.Identity.Name);
            Setting s = db.Settings.SingleOrDefault(x => x.settingID == vm.packageID);
            if (ModelState.IsValid && u != null && e != null && s != null)
            {
                //create new account fee object
                AccountFee accountFee = new AccountFee
                {
                    paymentDate = DateTime.Now,
                    durationDay = s.settingStringInt.Value,
                    fee = s.settingStringDouble.Value,
                    employeeID = u.Id
                };

                //update account expired date
                if (e.expiredOn >= DateTime.Now)
                {
                    e.expiredOn = e.expiredOn.Value.AddDays(accountFee.durationDay);

                }
                else
                {
                    e.expiredOn = DateTime.Now.AddDays(accountFee.durationDay);

                }

                db.AccountFees.Add(accountFee);
                db.SaveChanges();

                TempData["color"] = "alert-success";
                TempData["info"] = "Account fee updated successfully, your expired date will be on " + e.expiredOn;
            }
                return RedirectToAction("EmployeeProfile");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult EmployeeProfile()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            User u = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
            Employee e = db.Employees.SingleOrDefault(x => x.email == User.Identity.Name);

            ViewBag.u = u;
            ViewBag.e = e;
            return View();
        }

        //END of handle profile

        [Authorize(Roles = "Employee")]
        public ActionResult Dashboard()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult ListJob()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            //select
            //where it is posting or bided but not not bided by this user
            var post = db.Posts.Where(x => (x.status == "posting" || x.status == "bided") && 
            x.postExpired > DateTime.Now &&
            !x.RequestedLists.Any(y => y.User.email == User.Identity.Name));

            ViewBag.Posts = post.ToList().OrderByDescending(x => x.postedDate);
            ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
            return View();
        }

        [Authorize(Roles = "Employee")]
        public ActionResult ListRequestedJob()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            //select
            //the requested list that contains this user and status not success
            var requestedPost = db.RequestedLists.Where(x => 
                                            x.Post.status == "bided" &&
                                            x.Post.postExpired > DateTime.Now &&
                                            x.User.email == User.Identity.Name &&
                                            x.status != "success").ToList();

            ViewBag.requestedPosts = requestedPost.OrderByDescending(x => x.requestedDate);
            ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
            return View();
        }

        [Authorize(Roles = "Employee")]
        public ActionResult ListEmployment()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            //list all the job
            var requestedPost = db.RequestedLists.Where(x =>
                                            x.status == "success" &&
                                            x.Post.status == "working" &&
                                            x.Post.postExpired > DateTime.Now &&
                                            x.User.email == User.Identity.Name
                                            );
            string date = "";
            string successColor = "#00C851";

            foreach (var p in requestedPost)
            {
                string query;
                query = "{" +
                    "title: '" + p.Post.jobTitle + "'," +
                    "start: '" + p.Post.workingDate.Value.ToString("yyyy-MM-dd") + "'," +
                    "color: '" + successColor + "'" +
                    "},";

                //append to date
                date += query;
            }
            ViewBag.calendar = date;
            ViewBag.requestedPosts = requestedPost.ToList().OrderByDescending(x => x.requestedDate);
            ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
            return View();
        }

        [Authorize(Roles = "Employee")]
        public ActionResult JobHistory()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            var requestedPost = db.RequestedLists.Where(x =>
                                            (x.Post.status == "completed" || x.Post.status == "absent") &&
                                            x.User.email == User.Identity.Name &&
                                            x.status == "success");
            ViewBag.requestedPost = requestedPost.ToList().OrderByDescending(x => x.Post.workingDate);
            return View();
        }

        [Authorize(Roles = "Employee")]
        public ActionResult EmploymentDetail(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            RequestedList requestedPost = db.RequestedLists.SingleOrDefault(x =>
                                            x.requestedPostID == postID && 
                                            (x.Post.status == "completed" || x.Post.status == "absent") &&
                                            x.User.email == User.Identity.Name &&
                                            x.status == "success");
            if (requestedPost != null)
            {
                ViewBag.requestedPost = requestedPost;
                return View();
            }

            return RedirectToAction("JobHistory");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult WorkingMaps()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            DateTime dateBeforeADay = DateTime.Now.AddDays(-1);

            var requestedPost = db.RequestedLists.Where(x =>
                                            x.status == "success" &&
                                            x.Post.status == "working" &&
                                            x.User.email == User.Identity.Name
                                            );

            var workingPosts = db.RequestedLists.Where(x =>
                                            x.status == "success" &&
                                            x.Post.status == "reached" &&
                                            x.User.email == User.Identity.Name &&
                                            x.Post.workingDate > dateBeforeADay
                                            );
            ViewBag.requestedPosts = requestedPost.ToList().OrderBy(x => x.Post.workingDate);
            ViewBag.workingPosts = workingPosts.ToList().OrderBy(x => x.Post.workingDate);

            return View();
        }

        [Authorize(Roles = "Employee")]
        public ActionResult ReachedWorkPlace(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            DateTime dateBeforeADay = DateTime.Now.AddDays(-1);

            RequestedList requestedPost = db.RequestedLists.SingleOrDefault(x =>
                                            x.requestedPostID == postID &&
                                            x.status == "success" &&
                                            x.User.email == User.Identity.Name &&
                                            x.Post.status == "working" &&
                                            x.Post.workingDate > dateBeforeADay
                                            );
            if (postID != 0 && requestedPost != null)
            {
                requestedPost.Post.status = "reached";
                db.SaveChanges();
            }
            return RedirectToAction("WorkingMaps");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult JobDetail(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            ViewBag.self = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            Post post = db.Posts.SingleOrDefault(x => x.postID == postID && 
            x.postExpired > DateTime.Now &&
            x.status != "remove");
            if (postID != 0 && post != null)
            {
                ViewBag.post = post;
                ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
                
                //handle post report
                ViewBag.postReport = db.Settings.Where(x => x.type == "postReport" && x.status == "active");
                bool hasReported = false;
                if(db.PostReports.Any(x => x.reportedPost == post.postID && x.User.email == User.Identity.Name))
                {
                    hasReported = true;
                }
                ViewBag.hasReported = hasReported;

                //handle post biding close
                bool isBidClosed = db.Posts.Any(x => x.postID == postID && x.status != "posting" && x.status != "bided");
                ViewBag.isBidClosed = isBidClosed;

                PostReportVM vm = new PostReportVM
                {
                    postedID = post.postID
                };
                
                return View(vm);
            }
            return RedirectToAction("ListJob");
        }

        [HttpPost] // REPORT POST
        [Authorize(Roles = "Employee")]
        public ActionResult JobDetail(PostReportVM vm)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }

            ViewBag.self = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            int postID = vm.postedID;
            Post post = db.Posts.SingleOrDefault(x => x.postID == postID &&
            x.postExpired > DateTime.Now &&
            x.status != "remove");
            if (postID != 0 && post != null)
            {
                ViewBag.post = post;
                ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;

                //handle post report
                ViewBag.postReport = db.Settings.Where(x => x.type == "postReport" && x.status == "active");
                bool hasReported = false;
                if (db.PostReports.Any(x => x.reportedPost == post.postID && x.User.email == User.Identity.Name))
                {
                    hasReported = true;
                }
                ViewBag.hasReported = hasReported;

                //handle post biding close
                bool isBidClosed = db.Posts.Any(x => x.postID == postID && x.status != "posting" && x.status != "bided");
                ViewBag.isBidClosed = isBidClosed;

                //Add post report to db
                PostReport rpt = new PostReport
                {
                    reportDate = DateTime.Now,
                    reportedPost = vm.postedID,
                    reportType = vm.reportType,
                    comment = vm.comment,
                    reportByEmployee = db.Users.SingleOrDefault(x => x.email == User.Identity.Name).Id
                };
                db.PostReports.Add(rpt);
                db.SaveChanges();
                ViewBag.hasReported = true;
                TempData["info"] = "This post is reported successfully";
                return View(vm);
            }
            return RedirectToAction("ListJob");
        }

        [Authorize(Roles = "Employee")]
        public ActionResult RemoveReport(int postID = 0)
        {
            PostReport rpt = db.PostReports.SingleOrDefault(x => x.reportedPost == postID && x.User.email == User.Identity.Name);
            if(rpt != null)
            {
                db.PostReports.Remove(rpt);
                db.SaveChanges();
            }
            return RedirectToAction("JobDetail", new { postID = postID });
        }

        [Authorize(Roles = "Employee")]
        public ActionResult AddNewBid(int postID = 0, double wage = 0)
        {

            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("AddProfile");
            }
            //seach for db
            RequestedList rqt = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == postID && x.User.email == User.Identity.Name);
            if (postID != 0)
            {
                if (ValidationForWageBiding(wage, postID))
                {
                    if (rqt == null)
                    {
                        //new biding record
                        RequestedList newRqt = new RequestedList
                        {
                            requestedPostID = postID,
                            requestedEmployeeID = db.Users.SingleOrDefault(x => x.email == User.Identity.Name).Id,
                            wage = wage,
                            requestedDate = DateTime.Now,
                            status = "pending"
                        };
                        db.RequestedLists.Add(newRqt);
                    }
                    else
                    {
                        rqt.wage = wage;
                        rqt.requestedDate = DateTime.Now;
                    }
                    Post p = db.Posts.SingleOrDefault(x => x.postID == postID);
                    p.status = "bided";
                    db.SaveChanges();
                }
                else
                {
                    //invalid wage
                    TempData["info"] = "Your wage is out of range, please try again.";
                }
            }
            return RedirectToAction("JobDetail", new { postID = postID });
        }

        public bool ValidationForWageBiding(double wage, int inPostID)
        {
            double minWage = 1;
            double maxWage = db.Posts.SingleOrDefault(x => x.postID == inPostID).wage.Value;
            if(wage >= minWage && wage <= maxWage)
            {
                return true;
            }
            return false;
        }
        

        public string SavePhoto(HttpPostedFileBase f)
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

        public string SaveResume(HttpPostedFileBase f)
        {
            if (f != null)
            {
                string path = Server.MapPath("~/Files/Resume/");
                string extension = Path.GetExtension(f.FileName);
                string name = Guid.NewGuid().ToString("N") + extension;
                f.SaveAs(path + name);
                return name;
            }
            return "";
        }

        public bool isExpired(DateTime d)
        {
            return (d < DateTime.Now);
        }

        public DateTime getBidClosingDate(DateTime d)
        {
            int day = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt.Value;
            return d.AddDays(-day);

        }

        public bool hasProfile(string email)
        {
            return db.Employees.Any(x => x.email == email);
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
    }
}