using Newtonsoft.Json;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Replication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.CosmosDbReplicationModels.Customer
{
    public class CustomerModel : ReplicationModel
    {
        public override string ID { get; set; }

        public long CustomerID { get; set; }

        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool PhoneVerified { get; set; }
        public DateTime? PhoneVerifiedDate { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? EmailVerifiedDate { get; set; }
        public Genders Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public int TIQId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string TIQIdFormatted
        {
            get
            {
                var stringTIQId = new string('0', 9 - TIQId.ToString().Length) + TIQId.ToString();
                return Regex.Replace(stringTIQId, @"(\d{3})(?=\d)", "$1-"); ;
            }
        }

        /// <summary>
        /// Toyota Loyalty Program Black List
        /// </summary>
        public bool TLPBlackList { get; set; }

        public bool Blocked { get; set; }

        /// <summary>
        /// A schedule for automatically delete the customer after certain date,
        /// If customer wants to delete his profile
        /// </summary>
        public DateTime? DeleteOn { get; set; }

        public List<ShiftFileDTO>? Photos { get; set; }

        public CustomerAttributeModel JobTitle { get; set; }
        public CustomerAttributeModel Occupation { get; set; }
        public CustomerAttributeModel MaritalStatus { get; set; }
        public CustomerAttributeModel IncomeLevel { get; set; }

        /// <summary>
        /// It is reference to the city in the identity
        /// </summary>
        public string? CityID { get; set; }=default!;

        public string ItemType { get; set; }
    }
}
