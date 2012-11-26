namespace System.Web.Providers.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations; 
    using System.Runtime.CompilerServices;

    [Table("Profiles")]
    public class ProfileEntity
    {
        public DateTime LastUpdatedDate { get; set; }

        [Required]
        public string PropertyNames { get; set; }

        [Required, MaxLength]
        public byte[] PropertyValueBinary { get; set; }

        [Required]
        public string PropertyValueStrings { get; set; }

        public virtual System.Web.Providers.Entities.User User { get; set; }

        [ForeignKey("User"), Key]
        public Guid UserId { get; set; }
    }
}

