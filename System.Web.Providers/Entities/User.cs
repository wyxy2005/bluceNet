namespace System.Web.Providers.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations; 
    using System.Runtime.CompilerServices;

    public class User
    {
        public virtual System.Web.Providers.Entities.Application Application { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        public bool IsAnonymous { get; set; }

        public DateTime LastActivityDate { get; set; }

        public virtual MembershipEntity Membership { get; set; }

        public virtual ProfileEntity Profile { get; set; }

        [Key]
        public Guid UserId { get; set; }

        [StringLength(50), Required]
        public string UserName { get; set; }

        public virtual ICollection<UsersInRole> UsersInRoles { get; set; }
    }
}

