using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Services.ConnectionBroker
{
    [Table("rds.Server")]
    public partial class Server
    {
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        [StringLength(64)]
        public string OsVersion { get; set; }

        [StringLength(256)]
        public string ClusterName { get; set; }
    }
}