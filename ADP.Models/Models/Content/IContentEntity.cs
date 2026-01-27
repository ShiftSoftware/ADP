namespace ShiftSoftware.ADP.Models.Content
{
    public interface IContentEntity : IContentProps
    {
        public ContentBlob Content { get; set; }
    }
}
