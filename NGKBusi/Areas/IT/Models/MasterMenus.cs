using NGKBusi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace NGKBusi.Areas.IT.Models
{
    //public class V_IT_Master_Menus
    //{
    //    public int id { get; set; }

    //    public string name { get; set; }

    //    public int? parentID { get; set; }

    //    public int sequence { get; set; }

    //    public string action { get; set; }   // mapping ke view Reminder

    //    public string url => $"/IT/Reminder/{action}";

    //    public virtual V_IT_Master_Menus Parent { get; set; }

    //    public virtual ICollection<V_IT_Master_Menus> Children { get; set; }
    //}

    // DTO untuk JSON (biar lebih clean)
    public class MenuDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int Sequence { get; set; }
        public bool isActive { get; set; }
        public bool isGlobal { get; set; }
        public bool isShow { get; set; }
        public List<MenuDto> Children { get; set; }
    }

    public class AddMenuDto
    {
        public string name { get; set; }
        public int? parentID { get; set; }
        public string icon { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string area { get; set; }
        public string redirect { get; set; }
    }

    public class MenuOrderDto
    {
        public int Id { get; set; }
        public int? ParentId { get; set; } // null kalau root
        public int Order { get; set; }
    }


    public class ITConnection : DbContext
    {
        //public DbSet<V_IT_Master_Menus> V_IT_Master_Menus { get; set; }

        public ITConnection()
        {
            this.Database.Connection.ConnectionString =
                System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}
