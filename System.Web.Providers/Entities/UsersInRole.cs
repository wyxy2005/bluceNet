namespace System.Web.Providers.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations; 
    using System.Runtime.CompilerServices;

    public class UsersInRole
    {
        public virtual RoleEntity Role { get; set; }

        [Key, Column(Order=1), ForeignKey("Role")]
        public Guid RoleId { get; set; }

        public virtual System.Web.Providers.Entities.User User { get; set; }

        [ForeignKey("User"), Key, Column(Order=0)]
        public Guid UserId { get; set; }
    }
}

