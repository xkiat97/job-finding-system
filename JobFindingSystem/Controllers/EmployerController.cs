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
using Gnostice.StarDocsSDK;

namespace JobFindingSystem.Controllers
{
    public class EmployerController : Controller
    {
        DBEntities db = new DBEntities();
        PasswordHasher ph = new PasswordHasher();

        public bool hasProfile(string email)
        {
            return db.Users.Any(x => x.email == email && x.phone != null);
        }

        private void postStatusUpdate()
        {
            //rule 1: posting & bided => closing 
            var posts = db.Posts.Where(x => x.status == "posting" || x.status == "bided");
            foreach (Post post in posts)
            {
                //if expired
                if (getBidClosingDate(post.workingDate.Value) < DateTime.Now)
                {
                    post.status = "closing";
                }
            }
            db.SaveChanges();

        }
        private DateTime getBidClosingDate(DateTime d)
        {
            int day = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt.Value;
            return d.AddDays(-day);

        }

        // GET: Employer
        [Authorize(Roles = "Employer")]
        public ActionResult Index()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var myPosts = db.Posts.Where(x => x.User.email == User.Identity.Name &&
            x.Ratings.FirstOrDefault().ratingDate.Month == DateTime.Today.Month &&
            x.Ratings.FirstOrDefault().ratingDate.Year == DateTime.Today.Year);

            ViewBag.item1 = myPosts.Where(y => y.status == "completed").Sum(x => x.workingTotalWage)??0;

            ViewBag.item2 = myPosts.Where(x => x.status == "posting").Count();

            ViewBag.item3 = myPosts.Sum(x => x.PostFees.Sum(y => y.fee))??0;

            ViewBag.item4 = myPosts.Count(x => x.status == "reported");

            ViewBag.item5 = myPosts.Count(x => x.status == "completed");

            //calendar session
            string date = "";
            string successColor = "#00C851";
            string primaryColor = "#33b5e5";
            string warningColor = "#ffbb33";

            var posts = db.Posts.Where(x => x.User.email == User.Identity.Name);
            int dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt.Value;
            foreach (Post p in posts)
            {
                string query;
                if (p.status == "posting" || p.status == "bided")
                {

                    query = "{" +
                        "title: '" + p.jobTitle + "(Posting duration)" + "'," +
                        "start: '" + p.postedDate.Value.ToString("yyyy-MM-dd") + "'," +
                        "end: '" + p.postExpired.Value.ToString("yyyy-MM-dd") + "'," +
                        "color: '" + warningColor + "'" +
                        "},";

                    //append to date
                    date += query;
                }
                else if (p.status == "closing")
                {
                    query = "{" +
                        "title: '" + p.jobTitle + "(Closing)" + "'," +
                        "start: '" + p.workingDate.Value.AddDays(dayBeforeWork).ToString("yyyy-MM-dd") + "'," +
                        "color: '" + primaryColor + "'" +
                        "},";

                    //append to date
                    date += query;
                }
                else if (p.status == "working")
                {

                    query = "{" +
                        "title: '" + p.jobTitle + "(Working)" + "'," +
                        "start: '" + p.workingDate.Value.ToString("yyyy-MM-dd") + "'," +
                        "color: '" + successColor + "'" +
                        "},";

                    //append to date
                    date += query;
                }
            }
            ViewBag.calendar = date;
            ViewBag.u = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult Payment()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult PostingFeeHistory()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            ViewBag.p = db.PostFees.Where(x => x.Post.User.email == User.Identity.Name);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult RateHistory()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            ViewBag.r = db.Ratings.Where(x => x.Post.User.email == User.Identity.Name);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult EmployerProfile()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            User u = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            ViewBag.u = u;
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult viewEmployeeProfile(int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            User u = db.Users.SingleOrDefault(x => x.Id == employeeID);
            bool valid = db.Posts.Any(x => x.User.email == User.Identity.Name && x.RequestedLists.Any(y => y.requestedEmployeeID == employeeID));

            if (u != null && valid)
            {
                Employee e = db.Employees.SingleOrDefault(x => x.email == u.email);
                ViewBag.u = u;
                ViewBag.e = e;
                return View();
            }

            return RedirectToAction("Requestor");
        }

        [Authorize(Roles = "Employer")]
        public ActionResult viewEmployeePDF(int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            User u = db.Users.SingleOrDefault(x => x.Id == employeeID);
            bool valid = db.Posts.Any(x => x.User.email == User.Identity.Name && x.RequestedLists.Any(y => y.requestedEmployeeID == employeeID));

            if (u != null && valid)
            {
                string fileUrl = db.Employees.SingleOrDefault(x => x.email == u.email).resumeURL;
                string file = Server.MapPath("~/Files/Resume/"+ fileUrl);

                ViewerSettings viewerSettings = new ViewerSettings();
                viewerSettings.VisibleFileOperationControls.Open = true;
                ViewResponse viewResponse = MvcApplication.starDocs.Viewer.CreateView(
                    new FileObject(file), null, viewerSettings);

                return new RedirectResult(viewResponse.Url);
            }

            return RedirectToAction("Requestor");
        }

        [Authorize(Roles = "Employer")]
        public ActionResult UpdateProfile()
        {
            User user = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);

            var phoneCode = "";
            var phone = "";

            if (user.phone != null)
            {
                phoneCode = user.phone.Substring(0, 3);
                phone = user.phone.Substring(3);
            }

            if (user != null)
            {
                var relativePath = "~/Photo/Profile/" + user.image;
                var absolutePath = HttpContext.Server.MapPath(relativePath);

                if (!System.IO.File.Exists(absolutePath))
                {
                    user.image = "../noImage.png";
                }

                var vm = new ProfileVM
                {
                    Id = user.Id,
                    Name = user.userName,
                    Email = user.email,
                    PhotoURL = user.image,
                    phone = phone,
                    PhoneCode = phoneCode,
                    UserAddress = user.address
                };
                return View(vm);
            }
            return RedirectToAction("EmployerProfile");

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


        [HttpPost]
        [Authorize(Roles = "Employer")]
        public ActionResult UpdateProfile(ProfileVM vm)
        {
            User u = db.Users.SingleOrDefault(x => x.Id == vm.Id && x.email == User.Identity.Name);

            var phoneCode = "";
            var phone = "";

            if (u.phone != null)
            {
                phoneCode = u.phone.Substring(0, 3);
                phone = u.phone.Substring(3);
            }

            if (u != null)
            {
                //validation
                if (!VerifyPassword(u.password, vm.Password))
                {
                    ModelState.AddModelError("Password", "Password entered not matched with current password!");
                }

                if (!VerifyPhone(vm.PhoneCode, vm.phone))
                {
                    ModelState.AddModelError("phone", "Invalid phone number format.");
                }

                if (vm.Photo != null) // Photo is optional
                {
                    string err = GetPhotoError(vm.Photo);
                    if (err != null)
                    {
                        ModelState.AddModelError("Photo", err);
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

                    if (vm.NewPassword != null && vm.Password != null) // password optional
                    {
                        string hash = HashPassword(vm.NewPassword);
                        u.password = hash;
                    }

                    //change DB
                    u.userName = vm.Name.Trim();
                    u.phone = vm.PhoneCode.Trim() + vm.phone.Trim();
                    u.address = vm.UserAddress;

                    db.SaveChanges();
                    Session["PhotoURL"] = u.image;

                    TempData["color"] = "alert-success";
                    TempData["info"] = "Personal profile update successfully";
                    return RedirectToAction("EmployerProfile");
                }

            }
            return View(vm);
        }

        [Authorize(Roles = "Employer")]
        public ActionResult Employ(int postID = 0, int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            RequestedList rqst = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == postID && 
            x.Post.status == "bided" &&
            x.Post.postExpired > DateTime.Now &&
            x.requestedEmployeeID == employeeID);
            if (rqst != null)
            {
                //set all the requested list to the failed
                var rqstList = db.RequestedLists.Where(x => x.requestedPostID == postID);
                foreach (RequestedList item in rqstList)
                {
                    item.status = "failed";
                    item.confirmedDate = DateTime.Now;
                }
                db.SaveChanges();

                //set only one to success
                rqst.status = "success";
                //change the post status
                rqst.Post.status = "working";
                db.SaveChanges();
            }
            return RedirectToAction("PostDetail", new { postID = postID});
        }

        [Authorize(Roles = "Employer")]
        public ActionResult DELEmploy(int postID = 0, int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            RequestedList rqst = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == postID &&
            x.Post.status == "working" &&
            x.Post.postExpired > DateTime.Now &&
            x.requestedEmployeeID == employeeID);
            if (rqst != null)
            {
                //set all the requested list to the failed
                var rqstList = db.RequestedLists.Where(x => x.requestedPostID == postID);
                foreach (RequestedList item in rqstList)
                {
                    item.status = "pending";
                    item.confirmedDate = null;
                }

                //change the post status
                rqst.Post.status = "bided";
                db.SaveChanges();
            }
            return RedirectToAction("PostDetail", new { postID = postID });
        }

        [Authorize(Roles = "Employer")]
        public ActionResult PostDetail(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            if (db.Posts.Any(x => x.postID == postID && x.status == "saved"))
            {
                return RedirectToAction("AddPostFee", new { postID = postID});
            }
            Post post = db.Posts.SingleOrDefault(x => x.postID == postID && 
            x.postExpired > DateTime.Now &&
            (x.status == "posting" || x.status == "bided" || x.status == "working") &&
            x.User.email == User.Identity.Name);

            if (postID != 0 && post != null && isAvailable(post))
            {
                //if the post is not yet paying or expired
                if(post.status == "saved" || isExpired(post))
                {
                    //jump to payment
                    TempData["info"] = "Post is expired or not yet activated, please extend the posting period.";
                    return RedirectToAction("AddPostFee", "Employer", new { postID = post.postID });
                }
                ViewBag.post = post;
                ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
                return View();
            }
            return RedirectToAction("CurrentPost");
        }

        //LIST ALL the POST
        [Authorize(Roles = "Employer")]
        public ActionResult CurrentPost()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Where(x => 
                                x.User.email == User.Identity.Name &&
                                (x.status == "saved" || 
                                x.status == "posting")).ToList();
            ViewBag.Posts = post.OrderByDescending(x => x.postedDate);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult ExpiredPost()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Where(x =>
                                x.User.email == User.Identity.Name &&
                                (x.status == "expired")).ToList();
            ViewBag.Posts = post.OrderByDescending(x => x.postedDate);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult Requestor()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Where(x =>
                                x.User.email == User.Identity.Name &&
                                x.postExpired > DateTime.Now &&
                                (x.status == "bided" ||
                                x.status == "closing"));
            ViewBag.Posts = post.ToList().OrderByDescending(x => x.postedDate);
            ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
            
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult NewEmployment()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Where(x =>
                                x.User.email == User.Identity.Name &&
                                x.postExpired > DateTime.Now &&
                                (x.status == "working" ||
                                x.status == "reached"));
            ViewBag.Posts = post.ToList().OrderBy(x => x.postedDate);

            string date = "";
            string successColor = "#00C851";
            string primaryColor = "#33b5e5";
            string warningColor = "#ffbb33";

            foreach (var p in post)
            {
                string query;
                string color = warningColor;
                if(p.status == "working")
                {
                    color = primaryColor;

                }else if (p.status == "reached")
                {
                    color = successColor;
                }
                query = "{" +
                    "title: '" + p.jobTitle + "'," +
                    "start: '" + p.workingDate.Value.ToString("yyyy-MM-dd") + "'," +
                    "color: '" + color + "'" +
                    "},";

                //append to date
                date += query;
            }
            ViewBag.calendar = date;
            ViewBag.Posts = post.OrderByDescending(x => x.postedDate);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult WorkingDetail(int postID = 0, int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            RequestedList rqst = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == postID && 
            x.requestedEmployeeID == employeeID && 
            x.Post.User.email == User.Identity.Name &&
            x.Post.status == "reached");
            if (rqst != null)
            {
                Post post = rqst.Post;
                if (isAvailable(post))
                {
                    JobRatingVM vm = new JobRatingVM
                    {
                        postedID = postID,
                        employeeID = employeeID
                    };
                    ViewBag.post = post;
                    ViewBag.dayBeforeWork = db.Settings.SingleOrDefault(x => x.title == "dayBeforeWorking").settingStringInt;
                    ViewBag.requestedList = rqst;
                    return View(vm);
                }
            }

            return RedirectToAction("NewEmployment");
        }

        [HttpPost]
        [Authorize(Roles = "Employer")]
        public ActionResult WorkingDetail(JobRatingVM vm)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            RequestedList rqst = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == vm.postedID &&
            x.requestedEmployeeID == vm.employeeID &&
            x.Post.User.email == User.Identity.Name &&
            x.Post.status == "reached");
            Post post = rqst.Post;

            if (db.Ratings.Any(x => x.postID == vm.postedID && x.userID == vm.employeeID) || post == null)
            {
                return RedirectToAction("WorkingDetail", new { postID = vm.postedID, employeeID = vm.employeeID });
            }

            //add rating record
            Rating rating = new Rating
            {
                postID = vm.postedID,
                userID = vm.employeeID,
                nOfFive = vm.OutOfFiveRate,
                comment = vm.comment,
                ratingDate = DateTime.Now
            };
            db.Ratings.Add(rating);

            if (post.workingTotalWage == null)
            {
                //the working is absent
                post.workingTotalWage = 0;
                post.workingDuration = 0;
                post.workingStart = DateTime.Now;
                post.workingEnd = DateTime.Now;
                post.status = "absent";
                TempData["info"] = "Your employee is absent, action will be taken.";

            }
            else
            {
                //the working is attended and completed the job
                post.status = "completed";
                TempData["info"] = "Your employment is completed.";

            }

            db.SaveChanges();
            return RedirectToAction("EmploymentDetail", new { postID = vm.postedID});
        }

        [Authorize(Roles = "Employer")]
        public ActionResult WorkingStatusUpdate(int postID = 0, int employeeID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            RequestedList rqst = db.RequestedLists.SingleOrDefault(x => x.requestedPostID == postID && 
                                                                        x.requestedEmployeeID == employeeID && 
                                                                        x.Post.User.email == User.Identity.Name);
            if (rqst != null)
            {
                //Step 1 : check its status
                // stop -> start -> stop
                //if it is stop status -- start time & end time == null
                Post p = rqst.Post;
                if(p.workingStart == null)
                {
                    // start = null & end = null
                    //just start the job
                    p.workingStart = DateTime.Now;
                    p.workingEnd = null;
                }
                else if(p.workingEnd == null)
                {
                    // start = date & end = null
                    p.workingEnd = DateTime.Now;
                    //calculate total duration, complete a circle of time
                    double hrs = (p.workingEnd - p.workingStart).Value.TotalHours;

                    if (p.workingTotalWage == null || p.workingDuration == null)
                    {
                        p.workingDuration = hrs;
                        p.workingTotalWage = hrs * rqst.wage;
                    }
                    else
                    {
                        p.workingDuration = p.workingDuration.Value + hrs;
                        p.workingTotalWage = p.workingTotalWage.Value + (hrs * rqst.wage);
                    }
                }
                else
                {
                    // start = date & end = date
                    //reset it and initial start date
                    p.workingStart = DateTime.Now;
                    p.workingEnd = null;
                }
                db.SaveChanges();
            }
            return RedirectToAction("WorkingDetail", new { postID = postID, employeeID = employeeID });
        }

        [Authorize(Roles = "Employer")]
        public ActionResult JobHistory()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Where(x =>
                                x.User.email == User.Identity.Name &&
                                (x.status == "completed" || x.status == "absent"));
            ViewBag.Posts = post.ToList().OrderByDescending(x => x.postedDate);
            return View();
        }

        [Authorize(Roles = "Employer")]
        public ActionResult EmploymentDetail(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            Post post = db.Posts.SingleOrDefault(x => x.postID == postID && (x.status == "completed" || x.status == "absent") && x.User.email == User.Identity.Name);
            if(post != null)
            {
                ViewBag.post = post;
                return View();
            }

            return RedirectToAction("JobHistory");
        }

        //ADD POST
        [Authorize(Roles = "Employer")]
        public ActionResult AddPost()
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employer")]
        public ActionResult AddPost(AddPostVM vm)
        {
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            var jc = db.JobCategories.SingleOrDefault(j => j.jobID.ToString() == vm.jobCategory && j.status == "active");

            if (jc == null)
            {
                ModelState.AddModelError("jobCategory", "Invalid option! Select again.");
            }

            if (ModelState.IsValid)
            {
                var post = new Post
                {
                    jobTitle = vm.postTitle,
                    jobDesc = vm.postDesc,
                    jobImage = vm.Photo == null ? null : SavePostImage(vm.Photo),
                    workingAddress = vm.jobAddress,
                    workingDate = vm.workingDateTime,
                    wage = vm.expectedWage,
                    jobCategoryID = int.Parse(vm.jobCategory),

                    status = "saved",
                    employerID = db.Users.SingleOrDefault(
                        x => x.email == User.Identity.Name).Id,
                    modifiedDate = DateTime.Now
                };
                db.Posts.Add(post);
                db.SaveChanges();

                TempData["info"] = "New Post registered successfully, Please fill in the payment detail";
                return RedirectToAction("AddPostFee", "Employer", new {postID = post.postID });

            }
            return View(vm);
        }

        [Authorize(Roles = "Employer")]
        public ActionResult AddPostFee(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            var post = db.Posts.Find(postID);
            
            if(post == null || postID == 0 || !(post.status != "working" && post.status != "absent" && post.status != "completed" ))
            {
                return RedirectToAction("AddPost", "Employer");
            }
            ViewBag.posts = post;
            ViewBag.packages = db.Settings
                .Where(x => x.type == "postFee"
                && x.status == "active").ToList().OrderBy(y => y.settingStringInt); // it refer to day
            return View();

        }

        [HttpPost]
        [Authorize(Roles = "Employer")]
        public ActionResult AddPostFee(AddPostFeeVM vm)
        {
            int postID = int.Parse(vm.postID);
            var post = db.Posts.Find(postID);

            if (post == null || postID == 0 || !(post.status != "working" && post.status != "absent" && post.status != "completed"))
            {
                return RedirectToAction("AddPost");
            }

            if (ModelState.IsValid)
            {
                //update the first time post
                post.status = "posting";
                post.postedDate = post.postedDate??DateTime.Now;
                post.modifiedDate = DateTime.Now;

                //create new PostFee object
                // get current setting details
                Setting s = db.Settings.Find(int.Parse(vm.packageID.ToString()));
                PostFee postFee = new PostFee
                {
                    paymentDate = DateTime.Now,
                    durationDay = s.settingStringInt.Value,
                    fee = s.settingStringDouble.Value,
                    postID = postID
                };

                if (post.postExpired >= DateTime.Now)
                {
                    //if no yet expired
                    post.postExpired = post.postExpired.Value.AddDays(postFee.durationDay.Value);
                }
                else
                {
                    //update post expired date
                    post.postExpired = DateTime.Now.AddDays(postFee.durationDay.Value);
                }

                db.PostFees.Add(postFee);
                db.SaveChanges();

                TempData["info"] = "Posting fee updated successfully";
                return RedirectToAction("PostDetail", new { postID = post.postID });

            }
            
            ViewBag.posts = post;
            ViewBag.packages = db.Settings
                .Where(x => x.type == "postFee"
                && x.status == "active").ToList().OrderBy(y => y.settingStringInt);

            return View(vm);
        }

        [Authorize(Roles = "Employer")]
        public ActionResult UpdatePost(int postID = 0)
        {
            if (!hasProfile(User.Identity.Name))
            {
                return RedirectToAction("UpdateProfile");
            }

            if (postID == 0)
            {
                return RedirectToAction("AddPost");
            }
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            var post = db.Posts.SingleOrDefault(x => x.postID == postID && (x.status == "saved" || x.status == "posting"));
            if (post != null)
            {
                AddPostVM vm = new AddPostVM
                {
                    postID = post.postID,
                    postTitle = post.jobTitle,
                    postDesc = post.jobDesc,
                    PhotoURL = post.jobImage,
                    jobAddress = post.workingAddress,
                    workingDateTime = (DateTime)post.workingDate,
                    expectedWage = (double)post.wage                    
                };
                return View(vm);
            }
            return RedirectToAction("PostDetail", new { postID = post.postID });
        }

        [HttpPost]
        [Authorize(Roles = "Employer")]
        public ActionResult UpdatePost(AddPostVM vm)
        {
            ViewBag.JobCategory = db.JobCategories.Where(x => x.status == "active");

            if (ModelState.IsValid)
            {
                var post = db.Posts.SingleOrDefault(x => x.postID == vm.postID && (x.status == "saved" || x.status == "posting"));

                if (post != null)
                {
                    post.jobTitle = vm.postTitle;
                    post.jobDesc = vm.postDesc;
                    post.jobImage = vm.Photo == null ? vm.PhotoURL : SavePostImage(vm.Photo);
                    post.jobCategoryID = int.Parse(vm.jobCategory);
                    post.workingAddress = vm.jobAddress;
                    post.workingDate = vm.workingDateTime;
                    post.wage = vm.expectedWage;
                    post.modifiedDate = DateTime.Now;

                    db.SaveChanges();

                    TempData["info"] = "Post updated successfully.";
                    return RedirectToAction("PostDetail", new { postID = post.postID });
                }
                TempData["info"] = "Post data not found.";
            }
            else
            {
                TempData["info"] = "Invalid Input.";

            }
            return View(vm);
        }

        //Helper functions
        private string SavePostImage(HttpPostedFileBase f)
        {
            string path = Server.MapPath("~/Photo/Post/");
            string name = Guid.NewGuid().ToString("N") + ".jpg";

            var img = new ImageFactory()
                .Load(f.InputStream)
                .Resize(new ResizeLayer(new System.Drawing.Size(500, 500), ResizeMode.Crop))
                .Format(new JpegFormat())
                .Save(path + name);

            return name;
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
                .Resize(new ResizeLayer(new System.Drawing.Size(150, 150), ResizeMode.Crop))
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

        private bool isExpired(Post p)
        {
            int postingDay = (int)p.PostFees.Sum(x => x.durationDay);
            DateTime expiredDate = p.postedDate.Value.AddDays(postingDay);
            return (expiredDate < DateTime.Now);
        }
        
        private bool isAvailable(Post p)
        {
            return !(p.status == "reported" && p.status == "remove");
        }
    }
}