using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace NGKBusi.Areas.WebService.Models
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class Date
    {
        private DateTime? date_To;

        public Date(DateTime? date_To)
        {
            this.date_To = date_To;
        }

        public string @long { get; set; }
        public string @short { get; set; }
    }

    public class DisplayActivationDate
    {
        public Date date { get; set; }
        public Time time { get; set; }
    }

    public class DisplayBirthdayDate
    {
        public Date date { get; set; }
        public Time time { get; set; }
    }

    public class DisplayLastTransaction
    {
        public Date date { get; set; }
        public Time time { get; set; }
    }

    public class DisplayRegisteredDate
    {
        public Date date { get; set; }
        public Time time { get; set; }
    }

    public class Tada_BengkelPoints_MemberList
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public string cardNo { get; set; }
        public string programName { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string sex { get; set; }
        public int? age { get; set; }
        public string city { get; set; }
        public string address { get; set; }
        public string additionaldatastring { get; set; }
        //sfageg
        public double? balance { get; set; }
        public double? convertedBalance { get; set; }
        public int? countTransaction { get; set; }
        public int? averageSpending { get; set; }
        public string activationStoreLocation { get; set; }
        public int? totalTransaction { get; set; }
        public int? totalSpending { get; set; }
        public int? totalBalanceEarning { get; set; }
        public int? totalWalletEarning { get; set; }
        public int? totalBalanceRedeem { get; set; }
        public int? totalWalletRedeem { get; set; }
        public int? cardLevel { get; set; }
        public string cardStatus { get; set; }
        public string subscriptionStartDate { get; set; }
        public string subscriptionExpiredDate { get; set; }
        public int? revenueGeneratedFromReferral { get; set; }
        public string advocateName { get; set; }
        public int? totalShares { get; set; }
        public int? retentionRiskValue { get; set; }
        public int? sherlockScore { get; set; }


    }

    public class Time
    {
        public string @long { get; set; }
        public string @short { get; set; }
    }

    public class Wallet
    {
        public string unitType { get; set; }
        public DateTime? expiredAt { get; set; }
        public string amount { get; set; }
        public string walletName { get; set; }
        public string redeemRate { get; set; }
    }

    public class BengkelPointsConnection : DbContext
    {
        public DbSet<Tada_BengkelPoints_MemberList> Tada_BengkelPoints_MemberList { get; set; }

        public BengkelPointsConnection()
        {
            this.Database.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
    }
}