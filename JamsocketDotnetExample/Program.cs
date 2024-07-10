// This example demonstrates how to spawn a backend and connect to it using the Jamsocket API.
// The example requires the JAMSOCKET_TOKEN, JAMSOCKET_ACCOUNT, and JAMSOCKET_SERVICE environment variables to be set:
// - JAMSOCKET_TOKEN: The API token to use for authentication (find this on app.jamsocket.com by going to
//   settings -> access tokens and creating one)
// - JAMSOCKET_ACCOUNT: The account ID to spawn the backend under (must match the account of the token)
// - JAMSOCKET_SERVICE: The service ID to spawn.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        string token = Environment.GetEnvironmentVariable("JAMSOCKET_TOKEN") ?? "";
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("JAMSOCKET_TOKEN environment variable is not provided.");
        }
        string account = Environment.GetEnvironmentVariable("JAMSOCKET_ACCOUNT") ?? "";
        if (string.IsNullOrEmpty(account))
        {
            throw new Exception("JAMSOCKET_ACCOUNT environment variable is not provided.");
        }
        string service = Environment.GetEnvironmentVariable("JAMSOCKET_SERVICE") ?? "";
        if (string.IsNullOrEmpty(service))
        {
            throw new Exception("JAMSOCKET_SERVICE environment variable is not provided.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Spawn the backend
        var spawnResponse = await SpawnBackend(account, service);
        var spawnResult = JsonSerializer.Deserialize<SpawnResult>(spawnResponse);
        if (spawnResult == null)
        {
            throw new Exception("Attempt to spawn returned invalid JSON.");
        }

        if (string.IsNullOrEmpty(spawnResult.url) || string.IsNullOrEmpty(spawnResult.status_url))
        {
            throw new Exception("Attempt to spawn returned invalid URLs.");
        }

        // Wait for the backend to become ready
        await WaitForBackendReady(spawnResult.status_url);

        // Connect to the backend and print the result
        var result = await ConnectToBackend(spawnResult.url);
        Console.WriteLine(result);
    }

    private static async Task<string> SpawnBackend(string account, string service)
    {
        var spawnRequest = new
        {
            tag = "latest"
        };

        var content = new StringContent(JsonSerializer.Serialize(spawnRequest), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://api.jamsocket.com/user/{account}/service/{service}/spawn", content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task WaitForBackendReady(string statusUrl)
    {
        while (true)
        {
            var response = await client.GetAsync(statusUrl);
            response.EnsureSuccessStatusCode();

            var statusResult = JsonSerializer.Deserialize<StatusResult>(await response.Content.ReadAsStringAsync());

            if (statusResult == null)
            {
                throw new Exception("Attempt to get status returned invalid JSON.");
            }
            
            if (statusResult.state == "Ready")
            {
                break;
            }

            await Task.Delay(1000);
        }
    }

    private static async Task<string> ConnectToBackend(string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private class SpawnResult
    {
        public string? url { get; set; }
        public string? status_url { get; set; }
    }

    private class StatusResult
    {
        public string? state { get; set; }
    }
}
