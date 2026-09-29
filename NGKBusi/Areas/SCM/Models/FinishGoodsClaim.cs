using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.SCM.Models
{
    public class FinishGoodsClaim
    {
    }

    [Table("V_Search_Claim_By_BN")]
    public class V_Search_Claim_By_BN
    {
        public string FID { get; set; }
        public Int64? FNo { get; set; }
        public string FPM { get; set; }
        public decimal FQtyReal { get; set; }
        [Key]
        public string FScanBarcodeID { get; set; }
        public DateTime? FEntryDate { get; set; }
        public Int64? FCounter { get; set; }
        public string FThirdPartyID { get; set; }
        public string ThirdpartyName { get; set; }
        public string ThirdpartyAddress { get; set; }
        public string FTransportID { get; set; }
        public string FCreatedBy { get; set; }
        public DateTime? FCreatedDate { get; set; }
        public string FModifiedBy { get; set; }
        public DateTime? FModifiedDate { get; set; }
        public decimal? FHistoryDebitNum { get; set; }
        public string FHistoryTrNo { get; set; }
        public decimal? FHistoryNumID { get; set; }
        public DateTime? FHistoryEntryDate { get; set; }
        public string FHistorySectionID { get; set; }
        public string FHistoryPositionID { get; set; }
        public string FHistoryPM { get; set; }
        public string FHistoryMotorID { get; set; }
        public string FHistoryTypeBox { get; set; }
        public string FHistoryRackNo { get; set; }
        public decimal? FHistoryQtyReal { get; set; }
        public string FHistoryUserID { get; set; }
        public DateTime? FHistorySysDate { get; set; }
        public decimal? FHistoryIsFifo { get; set; }
        public string FHistoryScanBarcodeID { get; set; }
        public DateTime? FHistorySysDateReceived { get; set; }
        public string FDONo { get; set; }
        public string FRackNo { get; set; }
        public string FSendNo { get; set; }
        public string FHistorySendNo { get; set; }
        public string FNoLotProd { get; set; }
        public DateTime? FInspectionDate { get; set; }
        public string FNoIRD { get; set; }
        public string FInspector { get; set; }
        public string FHistoryDONo { get; set; }
        public string FHistoryThirdPartyID { get; set; }
        public string FHistoryTransportID { get; set; }
        public bool? FIsMixWithPlugCap { get; set; }
        public int? FGSN { get; set; }
        public string FCustRef { get; set; }
    }

    [Table("V_Search_Claim_Repacking")]
    public class V_Search_Claim_Repacking
    {
        [Key]
        public Int64 RowId { get; set; }
        public string FWillRepackIntoType { get; set; }
        public int FQty { get; set; }
        public string FLot { get; set; }
        public string FSavedStatus { get; set; }
        public string FPrintStatus { get; set; }
        public int FLabelStatus { get; set; }
        public string FLotRepacking { get; set; }
        public string FICNo { get; set; }
        public string FPartNo { get; set; }
        public int FSeqNo { get; set; }
        public string FScanBarcodeID { get; set; }
        public string FType { get; set; }
        public DateTime? FRequestDate { get; set; }


    }
    public class Tbl_ClaimByLotNoAndType
    {
        public string FPM { get; set; }
        public int Qty { get; set; }
        public string FEntryDate { get; set; }
        public string FHistorySysDateReceived { get; set; }
        public string ThirdpartyName { get; set; }
        public string ThirdpartyAddress { get; set; }
        public string LotNo { get; set; }
    }
    public class FinishGoodsClaimConnection : DbContext
    {
        public DbSet<V_Search_Claim_By_BN> V_Search_Claim_By_BN { get; set; }
        public DbSet<V_Search_Claim_Repacking> V_Search_Claim_Repacking { get; set; }
        //public DbSet<SCM_D365ImporForm_ProductReceipt> SCM_D365ImporForm_ProductReceipt { get; set; }
        //public DbSet<SCM_D365ImporForm_PurchaseBI> SCM_D365ImporForm_PurchaseBI { get; set; }
        //public DbSet<SCM_D365ImporForm_PurchaseLines> SCM_D365ImporForm_PurchaseLines { get; set; }
        //public DbSet<SCM_D365ImporForm_PurchaseBI_trial> SCM_D365ImporForm_PurchaseBI_trial { get; set; }
        //public DbSet<SCM_D365ImporForm_StockManagement> SCM_D365ImporForm_StockManagement { get; set; }
        public FinishGoodsClaimConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["BarcodeConnection"].ConnectionString;
        }
    }
}