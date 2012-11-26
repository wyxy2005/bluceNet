namespace System.Web.Providers.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations; 
    using System.Runtime.CompilerServices;

    [Table("Roles")]
    public class RoleEntity
    {
        public virtual System.Web.Providers.Entities.Application Application { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        [StringLength(0x100)]
        public string Description { get; set; }

        [Key, Column(Order=0)]
        public Guid RoleId { get; set; }

        [Required, StringLength(0x100)]
        public string RoleName { get; set; }

        public virtual ICollection<UsersInRole> UsersInRoles { get; set; }
    }
}

