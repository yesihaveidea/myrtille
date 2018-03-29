using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Enterprise
{
    public class HostAccessGroups
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public long HostID { get; set; }
        [ForeignKey("HostID")]
        public virtual Host Host { get; set; }

        public string AccessGroup { get; set; }
    }
}