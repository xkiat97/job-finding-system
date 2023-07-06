using JobFindingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JobFindingSystem.Controllers
{
    public class HomeController : Controller
    {
        DBEntities db = new DBEntities();

        public ActionResult Index()
        {
            //the home page for the system
            User usr = db.Users.SingleOrDefault(x => x.email == User.Identity.Name);
            if (usr != null)
            {
                switch (usr.role)
                {
                    case "SuperAdmin":
                        return RedirectToAction("Index", "Admin");
                    case "Admin":
                        return RedirectToAction("Index", "Admin");
                    case "Employee":
                        return RedirectToAction("Index", "Employee");
                    case "Employer":
                        return RedirectToAction("Index", "Employer");
                }
            }
            ViewBag.setting = db.Settings.Where(x => x.status == "active").ToList();
            ViewBag.post = db.Posts.ToList();
            ViewBag.user = db.Users.Where(x => x.status == "active" && (x.role == "Employer" || x.role == "Employee")).ToList();
            return View();
        }

        public ActionResult HowItWork()
        {
            //a static page for display about the system
            return View();
        }

        public ActionResult Pricing()
        {
            //a pricing plan for employee account & post 
            ViewBag.settings = db.Settings.Where(x => x.status == "active" && (x.type == "accountSubscribe" || x.type == "postFee"));
            return View();
        }

        public ActionResult Contact()
        {
            //a description of contact
            return View();
        }
        public ActionResult Termsofuse()
        {
            //a description of contact
            return View();
        }
    }
}