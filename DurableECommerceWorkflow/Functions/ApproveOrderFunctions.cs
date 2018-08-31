using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace DurableECommerceWorkflow
{
    public static class ApproveOrderFunctions
    {
        [FunctionName("ApproveOrderById")]
        public static async Task<IActionResult> ApproveOrderById(
            [HttpTrigger(AuthorizationLevel.Function,
                "post", Route = "approve/{id}")]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            [Table(OrderEntity.TableName, OrderEntity.OrderPartitionKey, "{id}", Connection = "AzureWebJobsStorage")] OrderEntity order,
            TraceWriter log, string id)
        {
            log.Info($"Setting approval status of order {id}");

            if (order == null)
            {
                return new NotFoundResult();
            }

            var body = await req.ReadAsStringAsync(); // should be "Approved" or "Rejected"
            var status = JsonConvert.DeserializeObject<string>(body);
            await client.RaiseEventAsync(order.OrchestrationId, "OrderApprovalResult", status);

            return new OkResult();
        }

        [FunctionName("ApproveOrder")]
        public static async Task<IActionResult> ApproveOrder(
            [HttpTrigger(AuthorizationLevel.Function,
                "post", Route = null)]HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient client,
            TraceWriter log)
        {
            log.Info("Received an approval result.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var approvalResult = JsonConvert.DeserializeObject<ApprovalResult>(requestBody);
            await client.RaiseEventAsync(approvalResult.OrchestrationId, "OrderApprovalResult", approvalResult.Approved ? "Approved" : "Rejected");
            log.Info($"Approval Result for {approvalResult.OrchestrationId} is {approvalResult.Approved}");
            return new OkResult();
        }
    }
}