using Microsoft.SemanticKernel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChatAPI.Data;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace ChatAPI.Plugins;

public class CustomerDataPlugin
{
    private readonly CustomerData _customerData;
    private readonly ILogger<CustomerDataPlugin> _logger;


    public CustomerDataPlugin(CustomerData customerData, ILogger<CustomerDataPlugin> logger)
    {
        _customerData = customerData;
        _logger = logger;
       
    }

    [KernelFunction("get_customer_data")]
    public async Task<string> GetCustomerDataAsync(string customer_id)
    {
        try
        {   
            
            _logger.LogInformation("Retrieving customer data for CustomerId: {CustomerId}", customer_id);
            var customer = await _customerData.GetCustomerAsync(customer_id);
            customer.Add("paths",new List<string>(["https://sachatapidemo.blob.core.windows.net/images/temp_diagram.png"]));
            var customerJson = System.Text.Json.JsonSerializer.Serialize(customer);
            _logger.LogInformation("Customer data retrieved: {CustomerData}", customerJson);
            

            return customerJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer data for CustomerId: {CustomerId}", customer_id);
            throw;
        }
    }
}