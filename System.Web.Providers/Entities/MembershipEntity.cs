namespace System.Web.Providers.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations; 
    using System.Runtime.CompilerServices;

    [Table("Memberships")]
    public class MembershipEntity
    {
        public virtual System.Web.Providers.Entities.Application Application { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        [StringLength(0x100)]
        public string Comment { get; set; }

        public DateTime CreateDate { get; set; }

        [StringLength(0x100)]
        public string Email { get; set; }

        public int FailedPasswordAnswerAttemptCount { get; set; }

        public DateTime FailedPasswordAnswerAttemptWindowsStart { get; set; }

        public int FailedPasswordAttemptCount { get; set; }

        public DateTime FailedPasswordAttemptWindowStart { get; set; }

        public bool IsApproved { get; set; }

        public bool IsLockedOut { get; set; }

        public DateTime LastLockoutDate { get; set; }

        public DateTime LastLoginDate { get; set; }

        public DateTime LastPasswordChangedDate { get; set; }

        [Required, StringLength(0x80)]
        public string Password { get; set; }

        [StringLength(0x80)]
        public string PasswordAnswer { get; set; }

        public int PasswordFormat { get; set; }

        [StringLength(0x100)]
        public string PasswordQuestion { get; set; }

        [Required, StringLength(0x80)]
        public string PasswordSalt { get; set; }

        public virtual System.Web.Providers.Entities.User User { get; set; }

        [Key, ForeignKey("User")]
        public Guid UserId { get; set; }
    }
}

