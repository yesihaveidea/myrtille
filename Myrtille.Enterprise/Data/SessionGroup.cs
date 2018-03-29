using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Myrtille.Enterprise
{
    public class SessionGroup
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public long SessionID { get; set; }
        [ForeignKey("SessionID")]
        public virtual Session Session { get; set; }

        [StringLength(250)]
        public string DirectoryGroup { get; set; }
    }
}