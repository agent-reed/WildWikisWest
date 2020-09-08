using Newtonsoft.Json;

namespace wildwikis.automation 
{
    public class PullRequestDto 
    {
        public string title {get; set;}

        public string body {get; set;}

        public string head {get; set;}

        [JsonProperty("base")]
        public string baseRef {get; set;}

        public bool draft {get {return true;} }

    }
}