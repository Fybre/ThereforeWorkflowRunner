using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThereforeWorkflowRunner.Models;

namespace ThereforeWorkflowRunner.Services
{
    public class ThereforeService
    {
        private readonly ILogger<ThereforeService> _logger;
        private const string EXECUTESINGLEQUERY_ENDPOINT = "/theservice/v0001/restun/ExecuteSimpleQuery";
        private const string STARTWORKFLOWINSTANCE_ENDPOINT = "/theservice/v0001/restun/StartWorkflowInstance";
        public ThereforeService(ILogger<ThereforeService> logger) 
        {
            _logger = logger;
        }

        public static async Task<List<int>> GetDocumentsForCategory(string tenant, string baseURL, string auth, int categoryNo, int fieldNo, string fieldCondition)
        {
            List<int> res = new List<int>();
            ThereforeExecuteSimpleQueryRequest request = new ThereforeExecuteSimpleQueryRequest() { CategoryNo = categoryNo, Condition = fieldCondition, FieldNo = fieldNo, OrderByFieldNo = fieldNo };
            var response = await SendThereforeRequest(tenant, baseURL + EXECUTESINGLEQUERY_ENDPOINT, auth, JsonConvert.SerializeObject(request));
            if (response.isSuccessful)
            {
                dynamic jobj = Newtonsoft.Json.JsonConvert.DeserializeObject(response.response) ?? "";
                var resultRows = jobj.QueryResult.ResultRows;
                if (resultRows != null)
                {
                    foreach (var resultRow in resultRows)
                    {
                        if (int.TryParse(resultRow.DocNo.ToString(), out int docNo) && docNo > 0)
                        {
                            res.Add(docNo);
                        }
                    }
                }
            }
            return res;
        }

        public static async Task<int?> StartWorkflowInstance(string tenant, string baseURL, string auth, int docNo, int worflowNo)
        {
            ThereforeStartWorkflowInstanceRequest request = new ThereforeStartWorkflowInstanceRequest() { DocNo = docNo, ProcessNo = worflowNo };
            var response =  await SendThereforeRequest(tenant, baseURL + STARTWORKFLOWINSTANCE_ENDPOINT, auth, JsonConvert.SerializeObject(request));
            if (response.isSuccessful)
            {
                var res = JObject.Parse(response.response); 
                Console.WriteLine(res["WorkflowInstanceNo"]);
                return Convert.ToInt32(res["WorkflowInstanceNo"]);
            }
            else
            {
                return 0;
            }
        }

        private static async Task<RESTResponse> SendThereforeRequest(string tenant, string requestURL, string auth, string bodyContent)
        {
            RESTResponse res = new RESTResponse();
            try{
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, requestURL);
                request.Headers.Add("TenantName", tenant);
                request.Headers.Add("Authorization", auth);
                var content = new StringContent(bodyContent, null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                res.isSuccessful = response.IsSuccessStatusCode;
                res.response = await response.Content.ReadAsStringAsync();
            }
            } catch (Exception ex)
            {
                res.isSuccessful = false;
                res.response = ex.Message;
            }
            return res;
        }
    }

    internal class RESTResponse 
    {
        public bool isSuccessful {get;set;} = false;
        public string response {get;set;} = "";
    }
}
