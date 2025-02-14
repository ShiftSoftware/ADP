using ShiftSoftware.ShiftEntity.Model.Replication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.CosmosDbReplicationModels.Customer
{
    public class CustomerLoginModel : ReplicationModel
    {
        public override string ID { get; set; }
        public long CustomerID { get; set; }
        public int AppVersionCode { get; set; }
        public Platforms Platform { get; set; }
        public Franchises Franchise { get; set; }
        public string LocalDeviceId { get; set; }
        public Languages ActiveLanguage { get; set; }
        public string PushNotificationToken { get; set; }
        public string SessionId { get; set; }
        public bool Verified { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool TransferredToNewLoginSystem { get; set; }
        public DateTime? TransferredToNewLoginSystemDate { get; set; }

        public string ItemType { get; set; }
    }
}
