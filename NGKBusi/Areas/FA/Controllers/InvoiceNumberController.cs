using NGKBusi.Areas.FA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Linq.Dynamic;

namespace NGKBusi.Areas.FA.Controllers
{
    public class InvoiceNumberController : Controller
    {
        private InvoiceNumberConnection dbs = new InvoiceNumberConnection();

        // GET: FA/InvoiceNumber
        public ActionResult Index()
        {
            return View();
        }
        // Helper classes for Tabulator
        public class TabulatorSorter
        {
            public string field { get; set; }
            public string dir { get; set; }
        }
        public class TabulatorFilter
        {
            public string field { get; set; }
            public string type { get; set; }
            public string value { get; set; }
        }
        // GET: FA/InvoiceNumber/GetSalesData
        [HttpPost]
        public ActionResult GetSalesDataDT()
        {
            var draw = Request.Form["draw"];
            var start = Convert.ToInt32(Request.Form["start"]);
            var length = Convert.ToInt32(Request.Form["length"]);
            var searchValue = Request.Form["search[value]"];

            var query = dbs.sales_D365ImporForm_Sales.AsQueryable();

            // Global search
            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(x =>
                    x.SalesOrder.Contains(searchValue) ||
                    x.CustomerName.Contains(searchValue) ||
                    x.ProductName.Contains(searchValue) ||
                    x.OriginalInvoiceNo.Contains(searchValue) ||
                    x.PackingSlipID.Contains(searchValue)
                );
            }

            // Per-column search (urutan sesuai DataTable)
            for (int i = 1; i <= 7; i++)
            {
                var colSearch = Request.Form[$"columns[{i}][search][value]"];
                if (!string.IsNullOrEmpty(colSearch))
                {
                    switch (i)
                    {
                        case 1: // PackingSlipID
                            query = query.Where(x => x.PackingSlipID.Contains(colSearch));
                            break;
                        case 2: // SalesOrder
                            query = query.Where(x => x.SalesOrder.Contains(colSearch));
                            break;
                        case 3: // CustomerName
                            query = query.Where(x => x.CustomerName.Contains(colSearch));
                            break;
                        case 4: // ProductName
                            query = query.Where(x => x.ProductName.Contains(colSearch));
                            break;
                        case 5: // PackingQty
                            if (int.TryParse(colSearch, out int qtyVal))
                                query = query.Where(x => x.PackingQty == qtyVal);
                            break;
                        case 6: // Date_Inv
                            if (DateTime.TryParse(colSearch, out DateTime dateVal))
                                query = query.Where(x => x.Date_Inv != null && x.Date_Inv.Value.Date == dateVal.Date);
                            else
                                query = query.Where(x => x.Date_Inv != null && x.Date_Inv.Value.ToString().Contains(colSearch));
                            break;
                        case 7: // OriginalInvoiceNo
                            query = query.Where(x => x.OriginalInvoiceNo.Contains(colSearch));
                            break;
                    }
                }
            }

            int recordsFiltered = query.Count();
            int recordsTotal = dbs.sales_D365ImporForm_Sales.Count();

            // Sorting
            var sortColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"]);
            var sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"];
            var sortDirection = Request.Form["order[0][dir]"]; // asc or desc

            if (!string.IsNullOrEmpty(sortColumn) && sortColumn != "null")
                query = query.OrderBy(sortColumn + " " + sortDirection);

            var data = query.Skip(start).Take(length).ToList();

            return Json(new
            {
                draw = draw,
                recordsFiltered = recordsFiltered,
                recordsTotal = recordsTotal,
                data = data
            }, JsonRequestBehavior.AllowGet);
        }
       
        [HttpPost]
        public JsonResult UpdateSalesRows(List<Sales_D365ImporForm_Sales> models)
        {
            if (models == null || models.Count == 0)
                return Json(new { success = false, message = "No data to update." });

            foreach (var model in models)
            {
                var entity = dbs.sales_D365ImporForm_Sales.Find(model.ID);
                if (entity != null)
                {
                    // Update hanya field yang diizinkan
                    entity.OriginalInvoiceNo = model.OriginalInvoiceNo;
                    // Pastikan parsing tanggal jika perlu
                    if (!string.IsNullOrEmpty(Convert.ToString(model.Date_Inv)))
                    {
                        DateTime parsedDate;
                        if (DateTime.TryParse(Convert.ToString(model.Date_Inv), out parsedDate))
                        {
                            entity.Date_Inv = parsedDate;
                        }
                    }
                }
            }
            dbs.SaveChanges();
            return Json(new { success = true , model = models});
        }
    }
}