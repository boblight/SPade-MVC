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
    
    public partial class Admin
    {
        public string AdminID { get; set; }
        public int ContactNo { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    
        public virtual Login Login { get; set; }
    }
}