//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SPade.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Submission
    {
        public int SubmissionID { get; set; }
        public string AdminNo { get; set; }
        public int AssgnID { get; set; }
        public decimal Grade { get; set; }
        public string FilePath { get; set; }
        public System.DateTime Timestamp { get; set; }
    
        public virtual Assignment Assignment { get; set; }
        public virtual Student Student { get; set; }
    }
}
