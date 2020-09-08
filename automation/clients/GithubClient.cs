using System.Text;
using System.Net;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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
            _client.BaseAddress = new Uri("https://api.github.com/repos/agent-reed/WildWikisWest/");
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AgentReedWikis", "1"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _client.DefaultRequestHeaders.Add("cache-control", "no-cache");
            _client.DefaultRequestHeaders.Add("Authorization", "token b1c55a9109a1d7cd371d00558055d0cc07bbe208");
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

            var detailsResponse = await _client.PostAsync("/repos/agent-reed/WildWikisWest/pulls", data);
            return detailsResponse.ToString();
        }

        public async Task<string> CreateFileInGit(WikipediaArticle article) {

            // First get the SHA of the branches current head
            var currentHead = await _client.GetStringAsync("git/refs/heads/submission-requests");
            dynamic json = JsonConvert.DeserializeObject(currentHead);
            var commitSha = json["object"]["sha"].ToString();

            // Use the current head to fetch the previous commit SHA
            var previousCommit = await _client.GetStringAsync($"git/commits/{commitSha}");
            json = JsonConvert.DeserializeObject(previousCommit);
            var treeSha = json["tree"]["sha"].ToString();

            // Upload content of the file to github server in prep for creating tree
            var blob = CreateNewPostBlob(article);
            var blobResponse = await _client.PostAsync("git/blobs", new StringContent(JsonConvert.SerializeObject(blob), Encoding.UTF8, "application/json"));
            var blobJsonString = await blobResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(blobJsonString);
            var blobSha = json["sha"].ToString();

            var tree = CreatePostTree(treeSha, blobSha);
            var treeResponse = await _client.PostAsync("git/trees", new StringContent(JsonConvert.SerializeObject(tree), Encoding.UTF8, "application/json"));
            var treeJsonString = await treeResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(treeJsonString);
            var newTreeSha = json["sha"].ToString();

            var commit = CreatePostCommit(commitSha, newTreeSha);
            var commitResponse = await _client.PostAsync("git/commits", new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
            var commitJsonString = await commitResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(commitJsonString);
            var newCommitSha = json["sha"].ToString();

            var updateRef = CreateUpdateRef(newCommitSha);
            await _client.PatchAsync("git/refs/heads/submission-requests", new StringContent(JsonConvert.SerializeObject(updateRef), Encoding.UTF8, "application/json"));

            return blobSha;

        }

        private BlobCreateDto CreateNewPostBlob(WikipediaArticle article) {
            BlobCreateDto blob = new BlobCreateDto();
            blob.content = "Cee Sharp Blob";
            return blob;
        }

        private TreeCreateDto CreatePostTree(string baseTreeSha, string blobSha) {
            TreeCreateDto tree = new TreeCreateDto();
            tree.base_tree = baseTreeSha;
            TreeItem newTreeItem = new TreeItem();
            newTreeItem.path = $"_posts/new-auto-post-{blobSha.Substring(0,8)}";
            newTreeItem.mode = "100644";
            newTreeItem.type = "blob";
            newTreeItem.sha = blobSha;
            tree.tree = new TreeItem[] { newTreeItem };

            return tree;
        }

        private CommitDto CreatePostCommit(string commitSha, string newTreeSha) {
            CommitDto commit = new CommitDto();
            commit.message = "Automated commit from Submission Request";
            commit.parents = new string[] {commitSha};
            commit.tree = newTreeSha;
            return commit;
        }

        private UpdateRefDto CreateUpdateRef(string newCommitSha) {
            UpdateRefDto updateRef = new UpdateRefDto();
            updateRef.sha = newCommitSha;
            return updateRef;
        }
    }
}