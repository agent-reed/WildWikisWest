using System.Text;
using System.Net;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wildwikis.automation 
{
    public class GithubClient 
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public GithubClient(ILogger logger)
        {
            _logger = logger;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.github.com");
        }

        public async Task<string> OpenNewPullRequest()
        {
            PullRequestDto prDto = new PullRequestDto();
            prDto.title = "Test PR from WildWikis";
            prDto.body = "Test body";
            prDto.head = "HEAD";
            prDto.baseRef = "master";

            var json = JsonConvert.SerializeObject(prDto);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Add("Authorization", "token abc123");

            var detailsResponse = await _client.PostAsync("/repos/agent-reed/WildWikisWest/pulls", data);
            return detailsResponse.ToString();
        }
    }
}