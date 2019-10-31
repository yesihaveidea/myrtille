using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Services.ConnectionBroker
{
    [Table("rds.Session")]
    public partial class Session
    {
        public Guid Id { get; set; }

        public Guid TargetId { get; set; }

        public long? UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string UserName { get; set; }

        [Required]
        [StringLength(256)]
        public string UserDomain { get; set; }

        public int SessionId { get; set; }

        public long CreateTime { get; set; }

        public long? DisconnectTime { get; set; }

        [StringLength(256)]
        public string InitialProgram { get; set; }

        public byte? ProtocolType { get; set; }

        public byte? State { get; set; }

        public int ResolutionWidth { get; set; }

        public int ResolutionHeight { get; set; }

        public int ColorDepth { get; set; }

        public virtual Target Target { get; set; }

        public virtual User User { get; set; }
    }
}