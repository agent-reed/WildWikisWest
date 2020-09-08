namespace wildwikis.automation 
{
    public class CommitDto 
    {
        public string message { get; set; }
        
        public string[] parents { get; set; }

        public string tree { get; set; }
    }
}