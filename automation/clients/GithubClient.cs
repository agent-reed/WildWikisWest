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

        public GithubClient(ILogger logger, string token)
        {
            _logger = logger;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.github.com/repos/agent-reed/WildWikisWest/");
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AgentReedWikis", "1"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _client.DefaultRequestHeaders.Add("cache-control", "no-cache");
            _client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
        }

        public async Task<string> UploadNewWikiPost(WikipediaArticle article) {
            string branchTitle = article.Title.ToLower() + "-" + DateTime.Now.Ticks.ToString();
            string newBranchSha = await CreateNewBranch(branchTitle);
            string blobSha = await CreateFileInGit(article, branchTitle);
            string details = await OpenNewPullRequest(article, branchTitle);

            _logger.LogInformation("\n > Completed Github Upload Process");

            return details;
        }

        private async Task<string> OpenNewPullRequest(WikipediaArticle article, string branchName)
        {
            PullRequestDto prDto = new PullRequestDto();
            prDto.title = $"Add {article.Title}";
            prDto.body = $"Automated Pull Request created on {DateTime.Now.ToShortDateString()}";
            prDto.head = $"{branchName}";
            prDto.baseRef = "master";

            var json = JsonConvert.SerializeObject(prDto);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var detailsResponse = await _client.PostAsync("pulls", data);

            _logger.LogInformation($"\n > Opened new pull request for {branchName}\n");

            return detailsResponse.ToString();
        }

        private async Task<string> CreateNewBranch(string branchName) {
            var masterRef = await _client.GetStringAsync("git/refs/heads/master");
            dynamic json = JsonConvert.DeserializeObject(masterRef);
            var masterSha = json["object"]["sha"].ToString();

            var branch = CreateBranch(branchName, masterSha);
            var branchResponse = await _client.PostAsync("git/refs", new StringContent(JsonConvert.SerializeObject(branch), Encoding.UTF8, "application/json"));
            var blobJsonString = await branchResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(blobJsonString);
            var newBranchSha = json["object"]["sha"].ToString();

            _logger.LogInformation($"\n > Completed Branch Creation - ({branchName})\n");

            return newBranchSha;
        }

        private async Task<string> CreateFileInGit(WikipediaArticle article, string newBranchName) {

            // First get the SHA of the branches current head
            var currentHead = await _client.GetStringAsync($"git/refs/heads/{newBranchName}");
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

            // Create a new git tree object containing the created blob and placement of new file
            var tree = CreatePostTree(treeSha, blobSha, article.Title.Replace(' ', '-'));
            var treeResponse = await _client.PostAsync("git/trees", new StringContent(JsonConvert.SerializeObject(tree), Encoding.UTF8, "application/json"));
            var treeJsonString = await treeResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(treeJsonString);
            var newTreeSha = json["sha"].ToString();

            // Create a commit object referencing the created tree and the previous commit
            var commit = CreatePostCommit(commitSha, newTreeSha);
            var commitResponse = await _client.PostAsync("git/commits", new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
            var commitJsonString = await commitResponse.Content.ReadAsStringAsync();
            json = JsonConvert.DeserializeObject(commitJsonString);
            var newCommitSha = json["sha"].ToString();

            // Update the head to point to the newly created commit
            var updateRef = CreateUpdateRef(newCommitSha);
            await _client.PatchAsync($"git/refs/heads/{newBranchName}", new StringContent(JsonConvert.SerializeObject(updateRef), Encoding.UTF8, "application/json"));

            _logger.LogInformation($"\n > Completed git file creation and commit - Blob SHA ({blobSha})\n");

            return blobSha;
        }

        private BlobCreateDto CreateNewPostBlob(WikipediaArticle article) {
            BlobCreateDto blob = new BlobCreateDto();
            blob.content = article.ToMarkdownPost();
            return blob;
        }

        private TreeCreateDto CreatePostTree(string baseTreeSha, string blobSha, string wikiTitle) {
            TreeCreateDto tree = new TreeCreateDto();
            tree.base_tree = baseTreeSha;
            TreeItem newTreeItem = new TreeItem();
            newTreeItem.path = $"_posts/{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-{wikiTitle}-{blobSha.Substring(0,8)}.md";
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

        private BranchCreateDto CreateBranch(string name, string masterSha) {
            BranchCreateDto branch = new BranchCreateDto();
            branch.branchRef = $"refs/heads/{name}";
            branch.branchSha = masterSha;
            return branch;
        }
    }
}