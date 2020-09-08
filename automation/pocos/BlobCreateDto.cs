namespace wildwikis.automation 
{
    public class BlobCreateDto 
    {
        public string content { get; set; }
        
        public string encoding { get { return "utf-8"; } }
    }
}