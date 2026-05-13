using System.Net.Http;
using System.Threading.Tasks;
using System;

var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
var response = await client.GetAsync("http://localhost:5032/api/iam/auth/google/start?tenantCode=dev&returnUrl=/app");
Console.WriteLine($"StatusCode: {response.StatusCode}");
foreach (var header in response.Headers)
{
    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
}
