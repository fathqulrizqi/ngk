using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NGKBusi.Areas.IT.Models
{
    [Table("IT_Disp_WH_Relation")]
    public class IT_Disp_WH_Relation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string disposal_id { get; set; }
        public string waste_handover_id { get; set; }
    }
}