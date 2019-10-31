using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Services.ConnectionBroker
{
    [Table("rds.Target")]
    public partial class Target
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Target()
        {
            Session = new HashSet<Session>();
            TargetIp = new HashSet<TargetIp>();
            TargetProperty = new HashSet<TargetProperty>();
        }

        public Guid Id { get; set; }

        public Guid EnvironmentId { get; set; }

        public int? PoolId { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [StringLength(256)]
        public string Fqdn { get; set; }

        [StringLength(15)]
        public string Netbios { get; set; }

        public byte? State { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Session> Session { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TargetIp> TargetIp { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TargetProperty> TargetProperty { get; set; }
    }
}