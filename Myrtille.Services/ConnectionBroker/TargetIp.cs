using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Services.ConnectionBroker
{
    [Table("rds.TargetIp")]
    public partial class TargetIp
    {
        public Guid Id { get; set; }

        public Guid TargetId { get; set; }

        [Required]
        [StringLength(256)]
        public string IpAddress { get; set; }

        public byte? Type { get; set; }

        public virtual Target Target { get; set; }
    }
}