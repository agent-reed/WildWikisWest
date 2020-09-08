namespace wildwikis.automation 
{
    public class UpdateRefDto 
    {
        public string sha { get; set; }
        
        public bool force { get { return true; } }
    }
}