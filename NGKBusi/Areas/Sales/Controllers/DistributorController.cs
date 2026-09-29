using BitMiracle.LibTiff.Classic;
using NGKBusi.Areas.Sales.Models;
using NGKBusi.Models;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NGKBusi.Areas.Sales.Controllers
{
    public class DistributorController : Controller
    {
        // GET: Sales/Distributor
        DefaultConnection db = new DefaultConnection();
        DistributorConnection dbDistributor = new DistributorConnection();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult PO()
        {
            return View();
        }
        public ActionResult POList()
        {
            return View();
        }

        public ActionResult MasterPart()
        {
            return View();
        }

        public class PODto
        {
            public int HeaderID { get; set; }
            public string DistroName { get; set; }
            public string DistroID { get; set; }
            public string PODate { get; set; }
            public string PONo { get; set; }
            public string SONo { get; set; }
            // List of arrays matching the grid columns
            public List<string[]> Lines { get; set; }
        }

        public ActionResult setMasterPart(string[][] iData)
        {
            var hashList = iData
        .Select(d => string.Concat(d.Take(6)).GetHashCode())
        .ToList();

            // Remove entries not in the incoming data
            var checkLineList = dbDistributor.Sales_Distributor_PO_Master_Part
                .Where(w => !hashList.Contains(w.HashCode))
                .ToList();
            dbDistributor.Sales_Distributor_PO_Master_Part.RemoveRange(checkLineList);

            foreach (var data in iData)
            {
                var hash = string.Concat(data.Take(6)).GetHashCode();

                var entity = dbDistributor.Sales_Distributor_PO_Master_Part
                    .FirstOrDefault(w => w.HashCode == hash);

                if (entity == null)
                {
                    entity = new Sales_Distributor_PO_Master_Part();
                    dbDistributor.Sales_Distributor_PO_Master_Part.Add(entity);
                }

                entity.Product_Name = string.IsNullOrEmpty(data[0]) ? null : data[0];
                entity.Vehicle_ID = string.IsNullOrEmpty(data[1]) ? null : data[1];
                entity.S_P_Type = string.IsNullOrEmpty(data[2]) ? null : data[2];
                entity.Item_ID = string.IsNullOrEmpty(data[3]) ? null : data[3];
                entity.Category = string.IsNullOrEmpty(data[4]) ? null : data[4];
                entity.Price = string.IsNullOrEmpty(data[5]) ? (int?)null : int.Parse(data[5]);
                entity.HashCode = hash;
            }

            dbDistributor.SaveChanges();
            return Json(iData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getMasterPart()
        {
            var _masterPartData = new List<object[]>();

            var _data = dbDistributor.Sales_Distributor_PO_Master_Part.Select(s => new { s.Product_Name, s.Vehicle_ID, s.S_P_Type, s.Item_ID, s.Category, s.Price }).ToList();
            if (_data.Count() > 0)
            {
                for (var i = 0; i < _data.Count(); i++)
                {
                    _masterPartData.Add(new object[] { _data[i].Product_Name, _data[i].Vehicle_ID, _data[i].S_P_Type, _data[i].Item_ID, _data[i].Category, _data[i].Price });
                }

            }
            return Json(_masterPartData, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult SavePO(PODto iData)
        {
            using (var transaction = dbDistributor.Database.BeginTransaction())
            {
                try
                {
                    Sales_Distributor_PO_Header header;

                    // --- STEP 1: Determine if New or Update ---
                    if (iData.HeaderID > 0)
                    {
                        // UPDATE EXISTING
                        header = dbDistributor.Sales_Distributor_PO_Header
                                    .FirstOrDefault(x => x.ID == iData.HeaderID);

                        if (header == null) return Json(new { success = false, message = "PO Not Found" });
                    }
                    else
                    {
                        // CREATE NEW
                        header = new Sales_Distributor_PO_Header();

                        // --- Generate Niterra_PO Logic (Only for NEW records) ---
                        string dateCode = DateTime.Now.ToString("yyyyMM");
                        string prefix = "NPO-" + dateCode + "-";

                        var lastPO = dbDistributor.Sales_Distributor_PO_Header
                            .Where(x => x.Niterra_PO.StartsWith(prefix))
                            .OrderByDescending(x => x.Niterra_PO)
                            .Select(x => x.Niterra_PO)
                            .FirstOrDefault();

                        int nextSequence = 1;
                        if (!string.IsNullOrEmpty(lastPO))
                        {
                            var parts = lastPO.Split('-');
                            if (parts.Length > 0 && int.TryParse(parts.Last(), out int currentSeq))
                            {
                                nextSequence = currentSeq + 1;
                            }
                        }
                        header.Niterra_PO = prefix + nextSequence.ToString("D4");
                        header.Created_At = DateTime.Now;

                        if (User.Identity.IsAuthenticated)
                            header.Created_By = User.Identity.Name;

                        dbDistributor.Sales_Distributor_PO_Header.Add(header);
                    }

                    // --- STEP 2: Update Header Fields (Common to Create and Update) ---
                    header.Cust_Name = iData.DistroName;
                    header.Cust_Code = iData.DistroID;
                    header.Distro_PO = iData.PONo;
                    header.Niterra_SO = iData.SONo;

                    if (DateTime.TryParseExact(iData.PODate, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        header.Date = parsedDate;
                    }

                    dbDistributor.SaveChanges(); // Save to ensure we have an ID

                    // --- STEP 3: Handle Lines (Delete Old -> Insert New) ---

                    // A. Remove existing lines for this header (Clean slate)
                    var existingLines = dbDistributor.Sales_Distributor_PO_Line
                                        .Where(x => x.Header_ID == header.ID).ToList();
                    if (existingLines.Count > 0)
                    {
                        dbDistributor.Sales_Distributor_PO_Line.RemoveRange(existingLines);
                    }

                    // B. Add new lines from Grid
                    if (iData.Lines != null && iData.Lines.Count > 0)
                    {
                        foreach (var lineData in iData.Lines)
                        {
                            if (string.IsNullOrEmpty(lineData[2])) continue; // Skip empty Product Name

                            var line = new Sales_Distributor_PO_Line();
                            line.Header_ID = header.ID; // Link to Header
                            line.Vehicle_Category = lineData[0];
                            line.S_P_Type = lineData[1];
                            line.Part_Name = lineData[2];
                            line.Part_Number = lineData[3];

                            int.TryParse(lineData[5], out int qty);
                            line.QTY = qty;
                            line.hash = string.Concat(lineData).GetHashCode();

                            dbDistributor.Sales_Distributor_PO_Line.Add(line);
                        }
                        dbDistributor.SaveChanges();
                    }

                    transaction.Commit();

                    // Return the ID so the frontend can update the hidden field
                    return Json(new
                    {
                        success = true,
                        message = "Data saved successfully!",
                        niterraPO = header.Niterra_PO,
                        headerID = header.ID // <--- Return the ID
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }


        public ActionResult setPOLine(string[][] iData)
        {
            var hashList = iData
        .Select(d => string.Concat(d.Take(6)).GetHashCode())
        .ToList();

            // Remove entries not in the incoming data
            var checkLineList = dbDistributor.Sales_Distributor_PO_Master_Part
                .Where(w => !hashList.Contains(w.HashCode))
                .ToList();
            dbDistributor.Sales_Distributor_PO_Master_Part.RemoveRange(checkLineList);

            foreach (var data in iData)
            {
                var hash = string.Concat(data.Take(6)).GetHashCode();

                var entity = dbDistributor.Sales_Distributor_PO_Master_Part
                    .FirstOrDefault(w => w.HashCode == hash);

                if (entity == null)
                {
                    entity = new Sales_Distributor_PO_Master_Part();
                    dbDistributor.Sales_Distributor_PO_Master_Part.Add(entity);
                }

                entity.Product_Name = string.IsNullOrEmpty(data[0]) ? null : data[0];
                entity.Vehicle_ID = string.IsNullOrEmpty(data[1]) ? null : data[1];
                entity.S_P_Type = string.IsNullOrEmpty(data[2]) ? null : data[2];
                entity.Item_ID = string.IsNullOrEmpty(data[3]) ? null : data[3];
                entity.Category = string.IsNullOrEmpty(data[4]) ? null : data[4];
                entity.Price = string.IsNullOrEmpty(data[5]) ? (int?)null : int.Parse(data[5]);
                entity.HashCode = hash;
            }

            dbDistributor.SaveChanges();
            return Json(iData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getPOLine()
        {
            var _masterPartData = new List<object[]>();

            var _data = dbDistributor.Sales_Distributor_PO_Master_Part.Select(s => new { s.Product_Name, s.Vehicle_ID, s.S_P_Type, s.Item_ID, s.Category, s.Price }).ToList();
            if (_data.Count() > 0)
            {
                for (var i = 0; i < _data.Count(); i++)
                {
                    _masterPartData.Add(new object[] { _data[i].Product_Name, _data[i].Vehicle_ID, _data[i].S_P_Type, _data[i].Item_ID, _data[i].Category, _data[i].Price });
                }

            }
            return Json(_masterPartData, JsonRequestBehavior.AllowGet);
        }

        public JsonResult setPOHeader()
        {
            var iDistroName = Request["iDistroName"];
            var iDistroID = Request["iDistroID"];
            var iPODate = Request["iPODate"];
            var iPONo = Request["iPONo"];
            var iSONo = Request["iSONo"];

            return Json(iDistroName, JsonRequestBehavior.AllowGet);
        }

        public class MasterPartData
        {
            public List<string> Data { get; set; }
            public List<string> SPType { get; set; }
            public List<string> PartNumber { get; set; }
            public List<string> Category { get; set; }
        }
        public JsonResult getMasterPartData()
        {
            var data = dbDistributor.Sales_Distributor_PO_Master_Part
        .Select(s => new { s.Product_Name, s.S_P_Type, s.Item_ID, s.Category })
        .ToList();

            var result = new MasterPartData
            {
                Data = data.Select(d => d.Product_Name).ToList(),
                SPType = data.Select(d => d.S_P_Type).ToList(),
                PartNumber = data.Select(d => d.Item_ID).ToList(),
                Category = data.Select(d => d.Category).ToList()
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPOHeaders()
        {
            var list = dbDistributor.Sales_Distributor_PO_Header
                .Select(x => new {
                    x.Niterra_PO,
                    x.Cust_Name,
                    x.Cust_Code,
                    Date = x.Date,
                    x.Distro_PO,
                    x.Niterra_SO
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }
}