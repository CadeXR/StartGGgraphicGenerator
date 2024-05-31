using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;

namespace StartGGgraphicGenerator
{
    public static class GitHubUploader
    {
        public static async Task UploadFileToGitHub(string content, string username, string repository, string branch, string filePath, string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);

                    var fileContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
                    var url = $"https://api.github.com/repos/{username}/{repository}/contents/{filePath}";

                    Console.WriteLine($"URL: {url}");
                    Console.WriteLine($"Username: {username}");
                    Console.WriteLine($"Repository: {repository}");
                    Console.WriteLine($"Branch: {branch}");
                    Console.WriteLine($"FilePath: {filePath}");

                    string sha = await GetFileShaIfExists(client, url);

                    var message = new
                    {
                        message = "Automatic update from StartGGgraphicGenerator",
                        committer = new
                        {
                            name = username,
                            email = $"{username}@users.noreply.github.com"
                        },
                        content = fileContent,
                        sha = sha,
                        branch = branch
                    };

                    var jsonContent = JsonConvert.SerializeObject(message);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(url, httpContent);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to upload to GitHub: {response.StatusCode} - {responseContent}");
                    }

                    MessageBox.Show("Uploaded to GitHub successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while uploading to GitHub: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static async Task<string> GetFileShaIfExists(HttpClient client, string url)
        {
            var getFileResponse = await client.GetAsync(url);
            if (getFileResponse.IsSuccessStatusCode)
            {
                var getFileContent = await getFileResponse.Content.ReadAsStringAsync();
                var existingFile = JsonConvert.DeserializeObject<dynamic>(getFileContent);
                return existingFile.sha;
            }
            return null;
        }
    }
}
