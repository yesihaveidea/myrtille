using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Services.ConnectionBroker
{
    [Table("rds.TargetProperty")]
    public partial class TargetProperty
    {
        public Guid Id { get; set; }

        public Guid TargetId { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        public string ValueStr { get; set; }

        public long? ValueInt { get; set; }

        public short? ValueType { get; set; }

        public virtual Target Target { get; set; }
    }
}