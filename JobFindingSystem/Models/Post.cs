//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JobFindingSystem.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Post
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Post()
        {
            this.PostFees = new HashSet<PostFee>();
            this.PostReports = new HashSet<PostReport>();
            this.Ratings = new HashSet<Rating>();
            this.RequestedLists = new HashSet<RequestedList>();
        }
    
        public int postID { get; set; }
        public string jobTitle { get; set; }
        public string jobDesc { get; set; }
        public string jobImage { get; set; }
        public Nullable<double> wage { get; set; }
        public Nullable<System.DateTime> postedDate { get; set; }
        public Nullable<System.DateTime> postExpired { get; set; }
        public string workingAddress { get; set; }
        public Nullable<System.DateTime> workingDate { get; set; }
        public Nullable<System.DateTime> workingStart { get; set; }
        public Nullable<System.DateTime> workingEnd { get; set; }
        public Nullable<double> workingDuration { get; set; }
        public Nullable<double> workingTotalWage { get; set; }
        public string status { get; set; }
        public Nullable<System.DateTime> modifiedDate { get; set; }
        public Nullable<int> jobCategoryID { get; set; }
        public Nullable<int> employerID { get; set; }
    
        public virtual JobCategory JobCategory { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PostFee> PostFees { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PostReport> PostReports { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Rating> Ratings { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RequestedList> RequestedLists { get; set; }
        public virtual User User { get; set; }
    }
}