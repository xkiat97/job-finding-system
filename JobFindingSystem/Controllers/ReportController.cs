using JobFindingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JobFindingSystem.Controllers
{
    public class ReportController : Controller
    {
        DBEntities db = new DBEntities();

        // GET: Report
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult AdminReport()
        {
            return View();
        }

        //Admin report
        public ActionResult UserInactiveXMonth(int day = 0)
        {
            DateTime d = DateTime.Now.AddDays(day);
            ViewBag.rpt = db.Users.Where(x => x.lastLogin < d);
            ViewBag.Title = "User Inactive Report ";
            return View();
        }

        public ActionResult UserMonthlyRegister(int month = 0, int year = 0)
        {
            if (month == 0 || year == 0)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }
            ViewBag.rpt = db.Users.Where(x => x.registeredDate.Month == month && x.registeredDate.Year == year);
            ViewBag.Title = "User Register Monthly Report ";
            ViewBag.subtitle = "in Month " + month + " Year " + year;
            return View();
        }

        public ActionResult UserEmployeeComplaintXTimes(int times = 0)
        {
            ViewBag.rpt = db.Users.Where(x => x.role == "Employer");
            ViewBag.Title = "Employee Complaint more than " + times + " times Report ";
            return View();
        }

        public ActionResult UserEmployeeRating()
        {
            ViewBag.rpt = db.Users.Where(x => x.role == "Employee");
            ViewBag.Title = "Employee Overall Rating Report";
            return View();
        }

        public ActionResult JobCategoryAvgWage()
        {
            ViewBag.rpt = db.Posts.ToList();
            ViewBag.Title = "Average job wage in Category";
            return View();
        }

        public ActionResult UserEmployerPostingStatus()
        {
            ViewBag.rpt = db.Users.Where(x => x.role == "Employer");
            ViewBag.Title = "Employer Posting Status Report";
            return View();
        }

        public ActionResult UserEmployerPendingPost()
        {
            ViewBag.rpt = db.Posts.Where(x => (x.status == "posting" || x.status == "bided" || x.status == "working" || x.status == "closing" || x.status == "reach"));
            ViewBag.Title = "Employer Pending Posts Report";
            return View();
        }

        public ActionResult TransactionAccountFee(int month = 0, int year = 0)
        {
            if (month == 0 || year == 0)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }
            ViewBag.rpt = db.AccountFees.Where(x => x.paymentDate.Month == month && x.paymentDate.Year == year);
            ViewBag.pkg = db.Settings.Where(x => x.type == "accountSubscribe").ToList();
            ViewBag.Title = "Monthly Account Subscription Report";
            ViewBag.subtitle = "in Month " + month + " Year " + year;

            return View();
        }

        public ActionResult TransactionPostFee(int month = 0, int year = 0)
        {
            if (month == 0 || year == 0)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }
            ViewBag.rpt = db.PostFees.Where(x => x.paymentDate.Value.Month == month && x.paymentDate.Value.Year == year);
            ViewBag.pkg = db.Settings.Where(x => x.type == "postFee").ToList();
            ViewBag.Title = "Monthly Posting Fee Report";
            ViewBag.subtitle = "in Month " + month + " Year " + year;

            return View();
        }

        public ActionResult EmployerEmployentTransactionDairy(int employerID = 0, int month = 0, int year = 0)
        {
            if (month == 0 || year == 0)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }
            List<Post> posts = db.Posts.Where(x => x.Ratings.FirstOrDefault().ratingDate.Month == month && x.Ratings.FirstOrDefault().ratingDate.Year == year).ToList();

            ViewBag.rpt = posts;
            ViewBag.Title = "Monthly " + posts.Count() + " Employment Fee Records Report";
            ViewBag.subtitle = "in Month " + month + " Year " + year;

            return View();
        }

        //JSON DATA
        public ActionResult Data()
        {
            var json = db.Skills.GroupBy(s => s.JobCategory.name).ToList().Select(g => new
            {
                jobcategory = g.Key,
                skills = g.Count()
            });
            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataAccountRole()
        {
            var json = db.Users.GroupBy(s => s.role).ToList().Select(g => new
            {
                role = g.Key,
                number = g.Count()
            });
            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataPostNJobCategory()
        {
            var json = db.Posts.GroupBy(s => s.JobCategory.name).ToList().Select(g => new
            {
                jobcategory = g.Key,
                number = g.Count()
            });
            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataMonthlyUserRegister(int year = 0)
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }
            var user = db.Users.Where(x => x.registeredDate.Year == year).GroupBy(s => s.registeredDate.Month).ToList().Select(g => new
            {
                date = new DateTime(year, g.Key, 1).ToString("MMM,yyyy"),
                admin = g.Count(x => x.role == "Admin"),
                employer = g.Count(x => x.role == "Employer"),
                employee = g.Count(x => x.role == "Employee")
            });
            var json = user;

            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataEmployeeSkillAnalyst(int employeeID = 0)
        {
            if (employeeID == 0)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            var temp = db.RequestedLists.Where(x => x.requestedEmployeeID == employeeID && x.status == "success");
            var json = temp.GroupBy(s => s.Post.JobCategory.name).ToList().Select(g => new
            {
                jobcategory = g.Key,
                totalProfit = Math.Round(g.Sum(x => x.Post.workingTotalWage).Value, 2)
            });

            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataEmployeeProfit(int employeeID = 0)
        {
            if (employeeID == 0)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            var temp = db.RequestedLists.Where(x => x.requestedEmployeeID == employeeID && x.status == "success");
            var json = temp.ToList().GroupBy(s => s.Post.Ratings.FirstOrDefault().ratingDate.Date).ToList().Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                totalProfit = Math.Round(g.Sum(x => x.Post.workingTotalWage).Value, 2)
            });

            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataEmployerPostNEmploy(int employerID = 0)
        {
            if (employerID == 0)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            int year = DateTime.Now.Year;
            var tempPostFee = db.PostFees.Where(x => x.Post.employerID == employerID && x.paymentDate.Value.Year == year)
                .GroupBy(s => s.paymentDate.Value.Month)
                .ToList()
                .Select(g => new ReportEmployerPostNEmployVM
                {
                    date = new DateTime(year, g.Key, 1).ToString("MMM,yyyy"),
                    postFee = Math.Round(g.Sum(x => x.fee).Value, 2),
                    employFee = 0
                });

            var tempEmployFee = db.Posts.Where(x => x.Ratings.FirstOrDefault().ratingDate.Year == year &&
                                                    x.employerID == employerID &&
                                                    x.status == "completed")
                                                    .ToList()
                                                    .GroupBy(s => s.Ratings.FirstOrDefault().ratingDate.Month).Select(g => new ReportEmployerPostNEmployVM
                                                    {
                                                        date = new DateTime(year, g.Key, 1).ToString("MMM,yyyy"),
                                                        postFee = 0,
                                                        employFee = Math.Round(g.Sum(x => x.workingTotalWage).Value, 2)
                                                    });
            var combine = tempEmployFee.Union(tempPostFee);

            var json = combine.GroupBy(s => s.date).Select(g => new ReportEmployerPostNEmployVM
            {
                date = g.Key,
                postFee = Math.Round(g.Sum(x => x.postFee), 2),
                employFee = Math.Round(g.Sum(x => x.employFee), 2)
            });

            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataPostingStatus(int employerID = 0)
        {
            if (employerID == 0)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            var json = db.Posts.Where(x => x.employerID == employerID).GroupBy(s => s.status).ToList().Select(g => new
            {
                status = g.Key,
                quantity = g.Count()
            });
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DataEmployerPostNJobCategory(int employerID = 0)
        {
            if (employerID == 0)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            var json = db.Posts.Where(x => x.employerID == employerID).GroupBy(s => s.JobCategory.name).ToList().Select(g => new
            {
                jobcategory = g.Key,
                number = g.Count()
            });
            return Json(json, JsonRequestBehavior.AllowGet);

        }

        public ActionResult DataAdminProfit()
        {
            var tempEmploy = db.Posts.Where(x => x.status == "completed").ToList().GroupBy(s => s.Ratings.FirstOrDefault().ratingDate.Date).Select(g => new ReportAdminVM
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                employFee = (double)g.Sum(x => x.workingTotalWage),
                postFee = 0,
                accountFee = 0
            });
            var tempPost = db.PostFees.ToList().GroupBy(s => s.paymentDate.Value.Date).Select(g => new ReportAdminVM
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                employFee = 0,
                postFee = (double)g.Sum(x => x.fee),
                accountFee = 0
            });
            var tempAccount = db.AccountFees.ToList().GroupBy(s => s.paymentDate.Date).Select(g => new ReportAdminVM
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                employFee = 0,
                postFee = 0,
                accountFee = (double)g.Sum(x => x.fee)
            });
            var json = tempEmploy.Union(tempPost.Union(tempAccount)).GroupBy(s => s.date).Select(g => new ReportAdminVM
            {
                date = g.Key,
                employFee = Math.Round(g.Sum(x => x.employFee), 2),
                postFee = Math.Round(g.Sum(x => x.postFee), 2),
                accountFee = Math.Round(g.Sum(x => x.accountFee), 2)
            });
            return Json(json, JsonRequestBehavior.AllowGet);

        }
    }
}