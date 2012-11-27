namespace Clowa.EFProviders
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.CompilerServices;

    public class Session
    {
        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }

        public int Flags { get; set; }

        public int LockCookie { get; set; }

        public DateTime LockDate { get; set; }

        public bool Locked { get; set; }

        [Key, StringLength(0x58)]
        public string SessionId { get; set; }

        [MaxLength]
        public byte[] SessionItem { get; set; }

        public int Timeout { get; set; }
    }
}
