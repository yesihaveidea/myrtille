using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Myrtille.Services.Contracts;

namespace Myrtille.Enterprise
{
    public class Host
    {
        [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [StringLength(250),DataType("varchar")]
        public string HostName { get; set; }

        public string HostAddress { get; set; }

        public SecurityProtocolEnum Protocol { get; set; }
    }
}