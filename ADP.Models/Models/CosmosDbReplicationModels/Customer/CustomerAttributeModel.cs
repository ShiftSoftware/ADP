using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Replication;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.CosmosDbReplicationModels.Customer
{
    public class CustomerAttributeModel : ReplicationModel
    {
        public override string ID { get; set; }
        public string Name { get; set; }
        public string ItemType { get; set; }
    }
}
