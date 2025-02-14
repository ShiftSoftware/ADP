namespace ShiftSoftware.ADP.Models
{
    public class CacheableCSV
    {
        /// <summary>
        /// This will contain the Computed SHA512 as Base64 string generated from the RAW CSV line
        /// </summary>
        [FileHelpers.FieldHidden]
        public string id { get; internal set; }
    }
}