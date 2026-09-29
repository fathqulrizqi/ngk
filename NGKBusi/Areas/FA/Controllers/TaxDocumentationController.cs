        using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NGKBusi.Areas.FA.Models;
using NGKBusi.Models;
using Newtonsoft.Json;
using System.Collections;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace NGKBusi.Areas.FA.Controllers
{
    public class TaxDocumentationController : Controller
    {
        TaxDocumentationConnection dbtd = new TaxDocumentationConnection();
        DefaultConnection db = new DefaultConnection();
        // GET: FA/TaxDocumentation
        public ActionResult Index()
        {
            return View();
        }
        public class categoriesChart
        {
            public string categoriesName { get; set; }
        }
        public class ListChart
        {
            public string name { get; set; }
            public decimal value { get; set; }
        }

        public class ListDonutChart
        {
            public string name { get; set; }
            public decimal y { get; set; }
            public string color { get; set; }
        }

        public class ChartSeries
        {
            public string type { get; set; }
            public string name { get; set; }
            public List<decimal> data { get; set; }
        }

        [HttpPost]
        public ActionResult GetDataBarChart()
        {
            var header = dbtd.FA_TaxDocumentaion_Header.Where(w => w.Status == "ApproveResult").OrderByDescending(o => o.TaxAuditDate).ToList();
            // Simulasi data (gantilah dengan logika pengambilan data dari database)
            List<ChartSeries> series = new List<ChartSeries>();
            var categories = new ArrayList();
            List<decimal> totalClaim = new List<decimal>();
            List<decimal> totalBeforeSPHP = new List<decimal>();
            List<decimal> totalSPHP = new List<decimal>();
            List<decimal> totalResult = new List<decimal>();

            foreach (var item in header)
            {
                categories.Add(item.DocumentationTitle);
                totalClaim.Add(item.TotalClaim);
                totalBeforeSPHP.Add(item.RefundBeforeSPHP);
                totalSPHP.Add(item.RefundSPHP);
                totalResult.Add(item.RefundResult);

            }

            series.Add(
                    new ChartSeries
                    {
                        type = "column",
                        name = "Total Claim",
                        data = totalClaim
                    });

            series.Add(
                    new ChartSeries
                    {
                        type = "column",
                        name = "Total Refund Before SPHP",
                        data = totalBeforeSPHP
                    });

            series.Add(
                    new ChartSeries
                    {
                        type = "column",
                        name = "Total Refund SPHP",
                        data = totalSPHP
                    });

            series.Add(
                    new ChartSeries
                    {
                        type = "column",
                        name = "Total Refund Result",
                        data = totalResult
                    });

            var data = new { categories, series };

            return Json(data);
        }
        [HttpPost]
        public ActionResult LastThreeYearsChart()
        {
            var header = dbtd.FA_TaxDocumentaion_Header.Where(w => w.Status == "ApproveResult").OrderByDescending(o => o.FinalDiscussionDate).Take(10).ToList();

            List<decimal> totalResult = new List<decimal>();
            var categories = new ArrayList();
            foreach (var item in header)
            {
                // cek total klaim
                if (item.TotalClaim == 0)
                {
                    // jika total claim = 0, maka total result diambil dari item.totalResult
                    categories.Add(item.DocumentationTitle);
                    totalResult.Add(item.TotalResult);
                } else
                {
                    // jika total claim != 0, maka total result diambil dari total refund (item.RefundResult)
                    categories.Add(item.DocumentationTitle);
                    totalResult.Add(item.RefundResult);
                }
                

            }
            List<ChartSeries> series = new List<ChartSeries>();
            series.Add(
                    new ChartSeries
                    {
                        name = "Total Result",
                        data = totalResult
                    });

            var data = new { categories, series };
            return Json(data);
        }

        [HttpPost]
        public ActionResult GetDonutChart()
        {
            var header = dbtd.FA_TaxDocumentaion_Header.Where(w => w.Status == "ApproveResult").OrderByDescending(o => o.FinalDiscussionDate).FirstOrDefault();
            var correctionList = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == header.HeaderID).ToList();

            List<ListDonutChart> seriesBeforeSPHP = new List<ListDonutChart>();
            List<ListDonutChart> seriesSPHP = new List<ListDonutChart>();
            List<ListDonutChart> seriesResult = new List<ListDonutChart>();

            foreach (var item in correctionList)
            {

                seriesBeforeSPHP.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.BeforeSPHP }
                );

                seriesSPHP.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.SPHP }
                );

                seriesResult.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.Result }
                );
            }


            return new JsonResult()
            {
                Data = new
                {
                    fiscalYear = header.DocumentationTitle,
                    seriesBeforeSPHP = seriesBeforeSPHP,
                    seriesSPHP = seriesSPHP,
                    seriesResult = seriesResult,
                    title = "Correction List " + header.DocumentationTitle

                }
            };
        }
        [HttpPost]
        public ActionResult GetSelectedDonutChart(int HeaderID)
        {
            var header = dbtd.FA_TaxDocumentaion_Header.Where(w => w.Status == "ApproveResult" && w.HeaderID == HeaderID).OrderByDescending(o => o.FinalDiscussionDate).FirstOrDefault();
            var correctionList = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == header.HeaderID).ToList();

            List<ListDonutChart> seriesBeforeSPHP = new List<ListDonutChart>();
            List<ListDonutChart> seriesSPHP = new List<ListDonutChart>();
            List<ListDonutChart> seriesResult = new List<ListDonutChart>();

            foreach (var item in correctionList)
            {

                seriesBeforeSPHP.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.BeforeSPHP }
                );

                seriesSPHP.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.SPHP }
                );

                seriesResult.Add(
                    new ListDonutChart { name = item.CorrectionList, y = item.Result }
                );
            }


            return new JsonResult()
            {
                Data = new
                {
                    fiscalYear = header.DocumentationTitle,
                    seriesBeforeSPHP = seriesBeforeSPHP,
                    seriesSPHP = seriesSPHP,
                    seriesResult = seriesResult,
                    title = "Correction List " + header.DocumentationTitle

                }
            };
        }
        public ActionResult Dashboard()
        {
            return View();
        }

        public ActionResult TaxDocumentationList()
        {

            List<Tbl_FA_TaxDocumentation> actions = new List<Tbl_FA_TaxDocumentation>();

            var spl = dbtd.V_FA_TaxDocumentation.OrderByDescending(o => o.HeaderID).ToList();
            var CountRow = dbtd.V_FA_TaxDocumentation.Count();

            foreach (var Item in spl)
            {
                var TaxDate = Convert.ToDateTime(Item.TaxAuditDate);
                var StrTaxDate = TaxDate.ToString("yyyy-MM-dd");

                var FinalDate = Convert.ToDateTime(Item.FinalDiscussionDate);
                var StrFinalDate = FinalDate.ToString("yyyy-MM-dd");
                var UrlAction = Url.Action("ViewDocumentation", "TaxDocumentation", new { area = "FA", HeaderID = Item.HeaderID });
                actions.Add(
                    new Tbl_FA_TaxDocumentation
                    {
                        HeaderID = Item.HeaderID,
                        DocumentationTitle = Item.DocumentationTitle,
                        Type = Item.Type,
                        TaxAuditDate = StrTaxDate,
                        FinalDiscussionDate = StrFinalDate,
                        TotalClaim = Item.TotalClaim.ToString("N2"),
                        BeforeSPHP = Item.TotalBeforeSPHP.ToString("N2"),
                        SPHP = Item.TotalSPHP.ToString("N2"),
                        Received = Item.TotalResult.ToString("N2"),
                        //Status = "<h5><span class=\"badge badge-success\"> " + Item.Status + " </span></h5>",
                        //Button = "<a href=\"" + UrlAction + "\"  title =\"view Detail\" class=\"btn btn-primary btn-sm\"><i class=\"fa fa-eye\"></i></a>&nbsp;<a  title=\"view Document\" class=\"btn btn-danger btn-sm\"><i class=\"fa fa-file\"></i></a>"
                        Button = "<a href=\"" + UrlAction + "\"  title =\"view Detail\" class=\"btn btn-primary btn-sm\"><i class=\"fa fa-eye\"></i></a> <button title =\"Preview Pie Chart\" data-id=\"" + Item.HeaderID + "\" class=\"PreviewPieChart btn btn-success btn-sm\"><i class=\"fa fa-pie-chart\"></i></button>"
                    });
            }

            return Json(new
            {
                rows = actions,
                totalNotFiltered = CountRow,
                total = CountRow,
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult NewDocumentation()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SubmitHeaderDocumentation(FA_TaxDocumentaion_Header item)
        {
            var DocumentationTitle = item.DocumentationTitle;
            var Type = item.Type;
            var TaxAuditDate = item.TaxAuditDate;
            var FinalDiscussionDate = item.FinalDiscussionDate;
            var TotalClaim = item.TotalClaim;


            FA_TaxDocumentaion_Header header = new FA_TaxDocumentaion_Header();

            header.DocumentationTitle = DocumentationTitle;
            header.Type = Type;
            header.TaxAuditDate = TaxAuditDate;
            header.FinalDiscussionDate = FinalDiscussionDate;
            header.TotalClaim = TotalClaim;
            header.Status = "0";
            header.IsDelete = 0;


            dbtd.FA_TaxDocumentaion_Header.Add(header);
            int i = dbtd.SaveChanges();
            int headerID = header.HeaderID;

            // insert all correction list and files list
            if (i > 0)
            {
                // insert correction list
                var spl = dbtd.FA_TaxDocumentation_Master_CorrectionList.Where(w => w.MotherID == 0).ToList();
                foreach (var itemCorrection in spl)
                {
                    FA_TaxDocumentation_CorrectionList CorrectionListItem = new FA_TaxDocumentation_CorrectionList();

                    CorrectionListItem.CorrectionList = itemCorrection.CorrectionListName;
                    CorrectionListItem.HaveChild = itemCorrection.HaveChild;
                    CorrectionListItem.motherID = itemCorrection.MotherID.ToString();
                    CorrectionListItem.HeaderID = headerID;

                    dbtd.FA_TaxDocumentation_CorrectionList.Add(CorrectionListItem);
                    dbtd.SaveChanges();
                    string childMotherID = CorrectionListItem.ID.ToString();

                    // insert child of correction list //
                    var child = dbtd.FA_TaxDocumentation_Master_CorrectionList.Where(w => w.MotherID == itemCorrection.CorrectionID).ToList(); // get child of parent correction list

                    foreach (var childCorrection in child)
                    {
                        FA_TaxDocumentation_CorrectionList CorrectionListItemChild = new FA_TaxDocumentation_CorrectionList();
                        CorrectionListItemChild.CorrectionList = childCorrection.CorrectionListName;
                        CorrectionListItemChild.HaveChild = childCorrection.HaveChild;
                        CorrectionListItemChild.motherID = childMotherID;
                        CorrectionListItemChild.HeaderID = headerID;

                        dbtd.FA_TaxDocumentation_CorrectionList.Add(CorrectionListItemChild);
                        dbtd.SaveChanges(); // save child to documentation correction list
                    }
                }
                // insert file list
                var qFileList = dbtd.FA_TaxDocumentation_Master_FileList.Where(w => w.IsActive == 1).ToList();
                foreach (var itemFiles in qFileList)
                {
                    FA_TaxDocumentation_Files FileList = new FA_TaxDocumentation_Files(); ;
                    FileList.FileName = itemFiles.FileName;
                    FileList.DocumentType = itemFiles.DocumentType;
                    FileList.HeaderID = headerID;
                    FileList.FileID = itemFiles.ID;

                    dbtd.FA_TaxDocumentation_Files.Add(FileList);
                    dbtd.SaveChanges(); // add file to db one by one
                }
                return Json(new
                {
                    status = "1",
                    msg = "Save Success"
                });
            } else
            {
                return Json(new
                {
                    status = "0",
                    msg = "failed to save data"
                });
            }
        }
        [HttpPost]
        public ActionResult UpdateDocumentationHeader(FA_TaxDocumentaion_Header item)
        {
            var DocumentationTitle = item.DocumentationTitle;
            var Type = item.Type;
            var TaxAuditDate = item.TaxAuditDate;
            var FinalDiscussionDate = item.FinalDiscussionDate;
            var TotalClaim = item.TotalClaim;

            var documentationHeader = dbtd.FA_TaxDocumentaion_Header.Where(w => w.HeaderID == item.HeaderID).FirstOrDefault();

            documentationHeader.DocumentationTitle = DocumentationTitle;
            documentationHeader.Type = item.Type;
            documentationHeader.TaxAuditDate = item.TaxAuditDate;
            documentationHeader.FinalDiscussionDate = item.FinalDiscussionDate;
            documentationHeader.TotalClaim = item.TotalClaim;

            int i = dbtd.SaveChanges();

            // insert all correction list and files list
            if (i > 0)
            {
                // insert correction list                
                return Json(new
                {
                    status = "1",
                    msg = "Save Success"
                });
            }
            else
            {
                return Json(new
                {
                    status = "0",
                    msg = "failed to save data"
                });
            }
        }
        [HttpGet]
        public ActionResult ViewDocumentation(int HeaderID)
        {
            var QueryHeader = dbtd.V_FA_TaxDocumentation.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
            ViewBag.DocumentationHeader = QueryHeader;

            ViewBag.HeaderID = HeaderID;

            // get correction list
            var queryCorrectionList = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == HeaderID && w.motherID == "0").ToList();
            // declare correction list
            List<Tbl_TaxDocumentation_CorrectionList> CorrectionLists = new List<Tbl_TaxDocumentation_CorrectionList>();
            foreach (var item in queryCorrectionList)
            {
                CorrectionLists.Add(
                    new Tbl_TaxDocumentation_CorrectionList
                    {
                        ID = item.ID,
                        HeaderID = item.HeaderID,
                        CorrectionList = item.CorrectionList,
                        motherID = item.motherID,
                        HaveChild = item.HaveChild,
                        BeforeSPHP = item.BeforeSPHP,
                        SPHP = item.SPHP,
                        Result = item.Result,
                        BeforeSPHP_CreateTime = item.BeforeSPHP_CreateTime,
                        SPHP_CreateTime = item.SPHP_CreateTime,
                        Result_CreateTime = item.Result_CreateTime,
                        BeforeSPHP_Status = item.BeforeSPHP_Status,
                        SPHP_Status = item.SPHP_Status,
                        Result_Status = item.Result_Status
                    }); ;

                if (item.HaveChild == 1)
                {
                    var QueryChildCorrectionList = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == HeaderID && w.motherID == item.ID.ToString()).ToList();
                    foreach (var child in QueryChildCorrectionList)
                    {
                        CorrectionLists.Add(
                        new Tbl_TaxDocumentation_CorrectionList
                        {
                            ID = child.ID,
                            HeaderID = child.HeaderID,
                            CorrectionList = child.CorrectionList,
                            motherID = child.motherID,
                            HaveChild = child.HaveChild,
                            BeforeSPHP = child.BeforeSPHP,
                            SPHP = child.SPHP,
                            Result = child.Result,
                            BeforeSPHP_CreateTime = child.BeforeSPHP_CreateTime,
                            SPHP_CreateTime = child.SPHP_CreateTime,
                            Result_CreateTime = child.Result_CreateTime,
                            BeforeSPHP_Status = child.BeforeSPHP_Status,
                            SPHP_Status = child.SPHP_Status,
                            Result_Status = child.Result_Status
                        });
                    }
                }
            }

            // get file documentation
            var query = from document in dbtd.FA_TaxDocumentation_Files
                        join masterDocument in dbtd.FA_TaxDocumentation_Master_FileList on document.FileID equals masterDocument.ID
                        where document.HeaderID == 3
                        select new Tbl_FA_TaxDocumentation_Files
                        {
                            ID = document.ID,
                            HeaderID = document.HeaderID,
                            FileName = document.FileName,
                            DocumentType = document.DocumentType,
                            FileLocation = document.FileLocation,
                            Tooltip = masterDocument.Tooltip
                        };

            var results = query.ToList(); // Eksekusi query dan ambil hasilnya
            var qGetFileDocumentation = dbtd.V_FA_TaxDocumentation_Files.Where(w => w.HeaderID == HeaderID).ToList();
            ViewBag.FileDocumentation = qGetFileDocumentation;

            ViewBag.CorrectionList = CorrectionLists;
           
            return View();
        }

        [HttpPost]
        public ActionResult SaveCorrectionListDocumentation()
        {
            var HeaderID = int.Parse(Request.Form.Get("HeaderID"));
            var correctionType = Request.Form.Get("correctionType");

            //get header information
            var qHeader = dbtd.FA_TaxDocumentaion_Header.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
            if (correctionType == "BeforeSPHP")
            {
                qHeader.TotalBeforeSPHP = Convert.ToDecimal(Request.Form.Get("TotalBeforeSPHP"));
            } else if (correctionType == "SPHP")
            {
                qHeader.TotalSPHP = Convert.ToDecimal(Request.Form.Get("TotalSPHP"));
            }
            else if (correctionType == "Result")
            {
                qHeader.TotalResult = Convert.ToDecimal(Request.Form.Get("TotalResult"));
            }
            dbtd.SaveChanges();

            var spl = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == HeaderID).ToList();

            int s = 0;

            var arlist = new ArrayList(); // recommended 
            var arlist2 = new ArrayList(); // recommended 
            foreach (var item in spl)
            {

                var correctionID = item.ID;
                var corrValue = Request.Form.Get(correctionID.ToString());
                arlist.Add(corrValue);
                arlist2.Add('"' + correctionID.ToString() + '"');
                var qCorrection = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.ID == correctionID).FirstOrDefault();
                if (corrValue != "")
                {
                    if (correctionType == "BeforeSPHP")
                    {
                        qCorrection.BeforeSPHP = Convert.ToDecimal(corrValue);
                    } else if (correctionType == "SPHP")
                    {
                        qCorrection.SPHP = Convert.ToDecimal(corrValue);
                    }
                    else if (correctionType == "Result")
                    {
                        qCorrection.Result = Convert.ToDecimal(corrValue);
                    }

                } else
                {
                    if (correctionType == "BeforeSPHP")
                    {
                        qCorrection.BeforeSPHP = 0;
                    }
                    else if (correctionType == "SPHP")
                    {
                        qCorrection.SPHP = 0;
                    }
                    else if (correctionType == "Result")
                    {
                        qCorrection.Result = 0;
                    }
                }

                if (correctionType == "BeforeSPHP")
                {
                    qCorrection.BeforeSPHP_CreateTime = DateTime.Now;
                    qCorrection.BeforeSPHP_Status = 1;
                }
                else if (correctionType == "SPHP")
                {
                    qCorrection.SPHP_CreateTime = DateTime.Now;
                    qCorrection.SPHP_Status = 1;
                }
                else if (correctionType == "Result")
                {
                    qCorrection.Result_CreateTime = DateTime.Now;
                    qCorrection.Result_Status = 1;
                }



                int i = dbtd.SaveChanges();
                if (i > 0)
                {
                    s++;
                }
            }
            if (s > 0)
            {
                return Json(new
                {

                    status = "1",
                    msg = "Save Success"
                });
            } else
            {
                return Json(new
                {
                    HeaderID = HeaderID,
                    tes = arlist,
                    tes2 = arlist2,
                    status = "0",
                    msg = "failed to save data"
                });
            }

        }
        [HttpPost]
        public ActionResult SubmitCorrection()
        {
            int HeaderID = int.Parse(Request.Form.Get("HeaderID"));
            var correctionType = Request.Form.Get("correctionType");

            var spl = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == HeaderID).ToList();
            int s = 0;

            var arlist = new ArrayList(); // recommended 
            var arlist2 = new ArrayList(); // recommended 
            foreach (var item in spl)
            {

                var correctionID = item.ID;
                var corrValue = Request.Form.Get(correctionID.ToString());
                arlist.Add(corrValue);
                arlist2.Add('"' + correctionID.ToString() + '"');
                var qCorrection = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.ID == correctionID).FirstOrDefault();
                if (corrValue != "")
                {
                    if (correctionType == "BeforeSPHP")
                    {
                        qCorrection.BeforeSPHP = Convert.ToDecimal(corrValue);
                    }
                    else if (correctionType == "SPHP")
                    {
                        qCorrection.SPHP = Convert.ToDecimal(corrValue);
                    }
                    else if (correctionType == "Result")
                    {
                        qCorrection.Result = Convert.ToDecimal(corrValue);
                    }

                }
                else
                {
                    if (correctionType == "BeforeSPHP")
                    {
                        qCorrection.BeforeSPHP = 0;
                    }
                    else if (correctionType == "SPHP")
                    {
                        qCorrection.SPHP = 0;
                    }
                    else if (correctionType == "Result")
                    {
                        qCorrection.Result = 0;
                    }
                }

                if (correctionType == "BeforeSPHP")
                {
                    qCorrection.BeforeSPHP_CreateTime = DateTime.Now;
                    qCorrection.BeforeSPHP_Status = 1;
                }
                else if (correctionType == "SPHP")
                {
                    qCorrection.SPHP_CreateTime = DateTime.Now;
                    qCorrection.SPHP_Status = 1;
                }
                else if (correctionType == "Result")
                {
                    qCorrection.Result_CreateTime = DateTime.Now;
                    qCorrection.Result_Status = 1;
                }
                int i = dbtd.SaveChanges();
                if (i > 0)
                {
                    s++;
                }
            }

            if (s > 0)
            {
                // insert task list
                var MasterApproval = dbtd.FA_TaxDocumentation_Master_Approval.Where(w => w.IsActive == 1).ToList();
                foreach (var itemApproval in MasterApproval)
                {
                    Task_List task = new Task_List();
                    task.Module = "ViewDocumentation";
                    task.ModuleID = HeaderID.ToString();
                    task.ModuleController = "TaxDocumentation";
                    task.ModuleArea = "FA";
                    task.TaskName = "Approval Tax Documentation";
                    task.IsActive = 1;
                    task.TaskForUser = itemApproval.ApprovalNIK;
                    task.ModuleParameter = "HeaderID";

                    db.Task_List.Add(task);
                    db.SaveChanges();
                }


                int taskID = 10;

                var qHeader = dbtd.FA_TaxDocumentaion_Header.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
                if (correctionType == "BeforeSPHP")
                {
                    qHeader.TotalBeforeSPHP = Convert.ToDecimal(Request.Form.Get("TotalBeforeSPHP"));
                    qHeader.RefundBeforeSPHP = Convert.ToDecimal(Request.Form.Get("TotalRefundBeforeSPHP"));
                    qHeader.Status = "SubmitBeforeSPHP";
                }
                else if (correctionType == "SPHP")
                {
                    qHeader.TotalSPHP = Convert.ToDecimal(Request.Form.Get("TotalSPHP"));
                    qHeader.RefundSPHP = Convert.ToDecimal(Request.Form.Get("TotalRefundSPHP"));
                    qHeader.Status = "SubmitSPHP";
                }
                else if (correctionType == "Result")
                {
                    qHeader.TotalResult = Convert.ToDecimal(Request.Form.Get("TotalResult"));
                    qHeader.RefundResult = Convert.ToDecimal(Request.Form.Get("TotalRefundResult"));
                    qHeader.Status = "SubmitResult";
                }
                qHeader.TaskID = taskID;

                int c = dbtd.SaveChanges();

                var spls = dbtd.FA_TaxDocumentation_CorrectionList.Where(w => w.HeaderID == HeaderID).ToList();
                if (correctionType == "BeforeSPHP")
                {
                    spls.ForEach(w => w.BeforeSPHP_Status = 2);

                }
                else if (correctionType == "SPHP")
                {
                    spls.ForEach(w => w.SPHP_Status = 2);
                }
                else if (correctionType == "Result")
                {
                    spls.ForEach(w => w.Result_Status = 2);
                }

                int i = dbtd.SaveChanges();
                if (i > 0)
                {
                    return Json(new
                    {

                        status = "1",
                        msg = "Submit Success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        HeaderID = HeaderID,
                        status = "0",
                        msg = "failed to submit data"
                    });
                }
            } else
            {
                return Json(new
                {
                    HeaderID = HeaderID,
                    status = "0",
                    msg = "failed to update data Header"
                });
            }

        }

        [HttpPost]
        public ActionResult ApproveCorrection()
        {
            var HeaderID = int.Parse(Request.Form.Get("HeaderID"));
            var ModuleID = HeaderID.ToString();
            var correctionType = Request.Form.Get("correctionType");
            var TaskID = Convert.ToInt32(Request.Form.Get("TaskID"));
            string ModuleParameter = "HeaderID";

            var qHeader = dbtd.FA_TaxDocumentaion_Header.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
            if (correctionType == "BeforeSPHP")
            {
                qHeader.Status = "ApproveBeforeSPHP";
                qHeader.IsApproveBeforeSPHP = 1;
            } else if (correctionType == "SPHP")
            {
                qHeader.Status = "ApproveSPHP";
                qHeader.IsApproveSPHP = 1;
            } else if (correctionType == "Result")
            {
                qHeader.Status = "ApproveResult";
                qHeader.IsApproveResult = 1;
            }

            int s = dbtd.SaveChanges();

            if (s > 0)
            {
                var tasklist = db.Task_List.Where(w => w.ModuleParameter == ModuleParameter && w.ModuleID == ModuleID).ToList();
                foreach (var itemTask in tasklist)
                {
                    int taskId = itemTask.ID;

                    var qtask = db.Task_List.Where(w => w.ID == taskId).FirstOrDefault();
                    qtask.IsActive = 0;
                    db.SaveChanges();


                }
                db.SaveChanges();

                return Json(new
                {

                    status = "1",
                    msg = "Submit Success"
                });
            }
            else
            {
                return Json(new
                {
                    HeaderID = HeaderID,
                    status = "0",
                    msg = "failed to submit data"
                });
            }

        }

        [HttpPost]
        public ActionResult RejectCorrection()
        {
            var HeaderID = int.Parse(Request.Form.Get("HeaderID"));
            var correctionType = Request.Form.Get("correctionType");
            var TaskID = Convert.ToInt32(Request.Form.Get("TaskID"));
            string ModuleParameter = "HeaderID";

            var qHeader = dbtd.FA_TaxDocumentaion_Header.Where(w => w.HeaderID == HeaderID).FirstOrDefault();
            if (correctionType == "BeforeSPHP")
            {
                qHeader.Status = "RejectBeforeSPHP";
            }
            else if (correctionType == "SPHP")
            {
                qHeader.Status = "RejectSPHP";
            }
            else if (correctionType == "Result")
            {
                qHeader.Status = "RejectResult";
            }
            qHeader.RejectNotes = Request.Form.Get("RejectNotes");
            int s = dbtd.SaveChanges();

            if (s > 0)
            {
                var tasklist = db.Task_List.Where(w => w.ModuleParameter == ModuleParameter && w.ModuleID == HeaderID.ToString()).ToList();
                foreach (var itemTask in tasklist)
                {
                    int taskId = itemTask.ID;

                    var qtask = db.Task_List.Where(w => w.ID == taskId).FirstOrDefault();
                    qtask.IsActive = 0;
                    db.SaveChanges();


                }

                return Json(new
                {

                    status = "1",
                    msg = "Submit Success"
                });
            }
            else
            {
                return Json(new
                {
                    HeaderID = HeaderID,
                    status = "0",
                    msg = "failed to submit data"
                });
            }
        }
        [HttpPost]
        public JsonResult UploadTaxDocument(HttpPostedFileBase fileTaxDocumentation, string DocumentType, int HeaderID)
        {
            HttpPostedFileBase file = Request.Files["fileTaxDocumentation"];
            
            if (fileTaxDocumentation.ContentLength > 0)
            {
                string extension = Path.GetExtension(fileTaxDocumentation.FileName);
                string filePath = Server.MapPath("~/Files/FA/TaxDocumentation/");
                string fileName = DocumentType + "_" + HeaderID + extension;
                filePath = filePath + fileName ;

                fileTaxDocumentation.SaveAs(filePath);

                var taxDocument = dbtd.FA_TaxDocumentation_Files.Where(w => w.HeaderID == HeaderID && w.DocumentType == DocumentType).FirstOrDefault();
                taxDocument.IsEnable = 1;
                //taxDocument.FileLocation = filePath;
                taxDocument.FileLocation = fileName;
                dbtd.SaveChanges();

                var jsonResult =
                 Json(new
                 {
                     status =  1,
                     fileName = fileName,
                     DocumentType = DocumentType,
                     headerid = HeaderID,
                     taxdocument = taxDocument,
                     sdoctype = DocumentType

                 }, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
            else
            {
                return Json(new
                {
                    status = 0,
                }, JsonRequestBehavior.AllowGet);
            }

        }
    }
    
}