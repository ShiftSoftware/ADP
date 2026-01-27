using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.Content
{
    public class ContentSection
    {
        public string id { get; set; }
        public int ContentID { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string MediaAssets { get; set; }
        public int SortOrder { get; set; }
        public string AdditionalData { get; set; }
    }
}
