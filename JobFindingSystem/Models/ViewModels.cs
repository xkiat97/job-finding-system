using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JobFindingSystem.Models
{
    // LOGIN ------------------------------------------------------------------
    public class LoginVM
    {
        [Required]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string password { get; set; }

        public bool RememberMe { get; set; }
    }

    // REGISTER ---------------------------------------------------------------
    public class AddAccountVM
    {
        [Required(ErrorMessage = "Email cannot be empty.")]
        [StringLength(50)]
        [EmailAddress]
        [System.Web.Mvc.Remote("CheckUserEmail", "Account", ErrorMessage = "Email already exist! Re-enter the email.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Name cannot be empty.")]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string username { get; set; }

        public string PhotoURL { get; set; }
        public HttpPostedFileBase Photo { get; set; }

        [Required(ErrorMessage = "Phone number cannot be empty.")]
        [RegularExpression(@"([0-9]{7,})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(8)]
        [Display(Name = "Phone")]
        public string phone { get; set; }

        [Required(ErrorMessage = "Phone code must be selected.")]
        [Display(Name = "Phone")]
        public string PhoneCode { get; set; }

        [Required(ErrorMessage = "Address cannot be empty.")]
        [StringLength(250)]
        [Display(Name = "Address")]
        public string address { get; set; }

        [Required(ErrorMessage = "Password cannot be empty.")]
        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string password { get; set; }

        [Required(ErrorMessage = "Confirm password cannot be empty.")]
        [StringLength(20, MinimumLength = 8)]
        [Compare("password")]//compare the password
        [Display(Name = "Confirm Password")]
        public string confirm { get; set; }


        [Display(Name = "Account Type")]
        public string role { get; set; }

    }

    public class UserRegisterVM
    {
        [Required]
        [StringLength(100)]
        [EmailAddress]
        [System.Web.Mvc.Remote("CheckUserEmail", "Account", ErrorMessage = "Dulplicated {0}.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string password { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 8)]
        [Compare("password")]//compare the password
        [Display(Name = "Confirm Password")]
        public string confirm { get; set; }

        [Required]
        [Display(Name = "Account Type")]
        public string role { get; set; }
    }

    // CHANGE -----------------------------------------------------------------
    public class ChangeVM
    {
        // TODO
        [Display(Name = "Current Password")]
        [Required]
        [StringLength(20, MinimumLength = 5)]
        public string current { get; set; }

        [Display(Name = "New Password")]
        [Required]
        [StringLength(20, MinimumLength = 5)]
        public string newpass { get; set; }

        [Display(Name = "Confirm Password")]//used for label
        [Required]
        [StringLength(20, MinimumLength = 5)]
        [Compare("New")]//compare the password
        public string confirm { get; set; }


    }


    // CHANGE PROFILE
    public class ProfileVM
    {
        [StringLength(50)]
        [Display(Name = "Name")]
        public string Name { get; set; }
        
        [StringLength(50)]
        [EmailAddress]
        public string Email { get; set; }

        public string PhotoURL { get; set; }
        public HttpPostedFileBase Photo { get; set; }

        [Display(Name = "User ID")]
        public int Id { get; set; }
        
        [RegularExpression(@"([0-9]{7,})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(8)]
        [Display(Name = "Phone")]
        public string phone { get; set; }
        
        [Display(Name = "Phone")]
        public string PhoneCode { get; set; }

        [Display(Name = "User Address")]
        public string UserAddress { get; set; }

        [Display(Name = "Registered Date")]
        public DateTime RegisteredDate { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Reported")]
        public int? ReportCount { get; set; }

        [Display(Name = "IP Address")]
        public string IP { get; set; }

        [Display(Name = "User Agent")]
        public string userAgent { get; set; }

        [Display(Name = "Account Type")]
        public string Role { get; set; }

        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Display(Name = "Login Failure")]
        public int LoginFailure { get; set; }
    }

    // CHANGE JOB CATEGORY
    public class AddJobCategoryVM
    {
        [Required(ErrorMessage = "Title cannot be empty.")]
        [Display(Name = "Title")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description cannot be empty.")]
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }

    // CHANGE JOB CATEGORY
    public class EditJobCategoryVM
    {
        [Required(ErrorMessage = "Title cannot be empty.")]
        [Display(Name = "Title")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description cannot be empty.")]
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Job Category ID")]
        public int Id { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }
    }

    // CHANGE POST
    public class EditPostVM
    {
        [Display(Name = "Post ID")]
        public int Id { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Working Date")]
        public DateTime? WorkingDate { get; set; }

        [Display(Name = "Original Wage")]
        public double? Wage { get; set; }

        [Display(Name = "Posted Date")]
        public DateTime? PostedDate { get; set; }

        [Display(Name = "Expired Date")]
        public DateTime? ExpiredDate { get; set; }

        [Display(Name = "Post Fee")]
        public double? PostFee { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Job Category")]
        public int? Category { get; set; }

        [Display(Name = "Posted By")]
        public int? PostedBy { get; set; }

        [Display(Name = "Working Address")]
        public string Address { get; set; }

        [Display(Name = "Rating")]
        public string Rating { get; set; }

        public string CategoryName { get; set; }

        public string PostedByEmail { get; set; }
    }

    //EMPLOYEE ROLE
    public class EmployeeProfile
    {
        public HttpPostedFileBase Photo { get; set; }
        public string photoURL { get; set; }

        [Required(ErrorMessage = "Name cannot be empty.")]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string name { get; set; }

        [Required(ErrorMessage = "IC first 6 number cannot be empty.")]
        [RegularExpression(@"([0-9]{6})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(6)]
        [Display(Name = "NRIC")]
        public string nric1 { get; set; }

        [Required(ErrorMessage = "IC middle 2 number cannot be empty.")]
        [RegularExpression(@"([0-9]{2})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(2)]
        [Display(Name = "NRIC")]
        public string nric2 { get; set; }

        [Required(ErrorMessage = "IC last 4 number cannot be empty.")]
        [RegularExpression(@"([0-9]{4})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(4)]
        [Display(Name = "NRIC")]
        public string nric3 { get; set; }

        [Required(ErrorMessage = "Tell us more about you.")]
        [StringLength(250)]
        [Display(Name = "About you")]
        public string intro { get; set; }

        [Required(ErrorMessage = "Phone number cannot be empty.")]
        [RegularExpression(@"([0-9]{7,})$", ErrorMessage = "Only number is allowed.")]
        [StringLength(8)]
        [Display(Name = "Phone")]
        public string phone { get; set; }

        [Required(ErrorMessage = "Phone code must be selected.")]
        [Display(Name = "Phone")]
        public string PhoneCode { get; set; }

        [Required(ErrorMessage = "Address cannot be empty.")]
        [StringLength(250)]
        [Display(Name = "Address")]
        public string address { get; set; }

        [Required(ErrorMessage = "DOB cannot be empty.")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Date of Birth")]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Gender must be selected.")]
        [Display(Name = "Gender")]
        public char gender { get; set; }

        [Display(Name = "Your resume")]
        public HttpPostedFileBase resume { get; set; }
        public string resumeURL { get; set; }

        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [StringLength(20, MinimumLength = 8)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

    }

    public class EmployeeSkill
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Skill title cannot be empty.")]
        [StringLength(50)]
        [Display(Name = "Skill title")]
        public string name { get; set; }

        [Required(ErrorMessage = "Skill description cannot be empty.")]
        [StringLength(300)]
        [Display(Name = "Description")]
        public string desc { get; set; }

        [Required(ErrorMessage = "Job category must be selected.")]
        [StringLength(50)]
        [Display(Name = "Job Category")]
        public string jobCategory { get; set; }
        public int jobCategoryID { get; set; }

        [Required(ErrorMessage = "Proficiency must be selected.")]
        [StringLength(50)]
        [Display(Name = "Proficiency")]
        public string proficiency { get; set; } //STATE : beginner, intermediate, advance

    }

    public class EmployeeAccountFeeVM
    {
        [Required]
        public int packageID { get; set; }
    }

    //EMPLOYER ROLE
    public class AddPostVM
    {
        public int postID { get; set; }
        public int jobCategoryID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Job Title")]
        public string postTitle { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Job Description")]
        public string postDesc { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Job Category")]
        public string jobCategory { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "Address")]
        public string jobAddress { get; set; }

        [Required]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Working Date")]
        public DateTime workingDateTime { get; set; }
        
        [Required]
        [Display(Name = "Wage/hour")]
        public double expectedWage { get; set; } // calculate in hour

        public HttpPostedFileBase Photo { get; set; }
        public string PhotoURL { get; set; }
    }

    public class AddPostFeeVM
    {
        [Required]
        public string packageID { get; set; }

        [Required]
        public string postID { get; set; }
    }

    public class PostReportVM
    {
        [Display(Name = "Report type ")]
        public string reportType { get; set; }

        [Display(Name = "Comment ")]
        public string comment { get; set; }

        public int postedID { get; set; }
    }

    public class JobRatingVM
    {
        [Display(Name = "Rate out of five ")]
        public int OutOfFiveRate { get; set; }

        [Display(Name = "Comment ")]
        public string comment { get; set; }

        public int postedID { get; set; }
        public int employeeID { get; set; }

    }

    public class ReportEmployerPostNEmployVM
    {
        public string date { get; set; }
        public double postFee { get; set; }
        public double employFee { get; set; }
    }

    public class ReportAdminVM
    {
        public string date { get; set; }
        public double employFee { get; set; }
        public double postFee { get; set; }
        public double accountFee { get; set; }
    }

    public enum Proficiency
    {
        Beginner,
        Intermediate,
        Advance
    }

    public enum AccountType
    {
        Admin,
        Employee,
        Employer
    }

    public enum Status
    {
        active,
        inactive
    }

    public enum RequestedStatus
    {
        success,
        failed,
        pending
    }

    public enum PostStatus
    {
        //posting state
        saved, // postfee == null
        posting, // postfee > 0 && postDate+DurationDay* > today - dayBeforeWorking
        bided, // bider > 0
        closing, // date between today - dayBeforeWorking && today #waiting for employer (infinity check workDay minus dayBe4Work- e cant view, r can view)

        // employment created
        working, // waiting for working
        reached,//employee is coming
        absent, // employee absent (infinity check on working day -  )
        completed, // everything complete


        reported, // post being ban by the admin

        remove // post being deleted by owner
    }

    public enum PaymentType
    {
        accountSubscribe,
        postFee
    }
}