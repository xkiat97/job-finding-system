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
    
    public partial class PostFee
    {
        public int postfeeID { get; set; }
        public Nullable<System.DateTime> paymentDate { get; set; }
        public Nullable<int> durationDay { get; set; }
        public Nullable<double> fee { get; set; }
        public Nullable<int> postID { get; set; }
    
        public virtual Post Post { get; set; }
    }
}
