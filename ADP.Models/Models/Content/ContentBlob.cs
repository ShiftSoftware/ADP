using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.Content
{
    public class ContentBlob
    {
        public string id { get; set; }
        public List<ContentSection> Sections { get; set; } = new();
        public string MediaAssets { get; set; }
        public string Attributes { get; set; }
        public string AdditionalData { get; set; }
    }
}
