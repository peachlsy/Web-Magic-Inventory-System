using System;
using System.Net.Http;
using System.Net.Http.Headers;
namespace WDTA2
{
    public class OrderHistoryApi
    {
        //Set WebAPI port number and get connection
        private const string ApiBaseUri = "http://localhost:10298";
        public static HttpClient InitializeClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(ApiBaseUri) };
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}