using Newtonsoft.Json;

namespace wildwikis.automation 
{
    public class BranchCreateDto 
    {
        [JsonProperty("ref")]
        public string branchRef { get; set; }
        
        [JsonProperty("sha")]
        public string branchSha { get; set; }
    }
}