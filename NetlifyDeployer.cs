using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StartGGgraphicGenerator
{
    public static class NetlifyDeployer
    {
        public static async Task DeployToNetlify(string siteId, string htmlContent, string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var content = new StringContent(htmlContent);
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

                    var response = await client.PutAsync($"https://api.netlify.com/api/v1/sites/{siteId}/files/index.html", content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<NetlifyResponse>(responseBody);

                    if (result != null && !string.IsNullOrEmpty(result.url))
                    {
                        Console.WriteLine($"Site deployed to: {result.url}");
                    }
                    else
                    {
                        throw new Exception("Failed to retrieve deployment URL.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while deploying to Netlify: {ex.Message}");
            }
        }

        private class NetlifyResponse
        {
            public string url { get; set; }
        }
    }
}
