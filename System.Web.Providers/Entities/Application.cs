namespace System.Web.Providers.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.CompilerServices;

    public class Application
    {
        [Key]
        public Guid ApplicationId { get; set; }

        [StringLength(0xeb), Required]
        public string ApplicationName { get; set; }

        [StringLength(0x100)]
        public string Description { get; set; }

        public virtual ICollection<MembershipEntity> Memberships { get; set; }

        public virtual ICollection<RoleEntity> Roles { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}

