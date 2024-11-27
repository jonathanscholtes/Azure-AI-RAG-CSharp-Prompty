> [!WARNING]  
> **This project is currently in active development and may contain breaking changes.**  
> Updates and modifications are being made frequently, which may impact stability or functionality. This notice will be removed once development is complete and the project reaches a stable release.


# Robust GenAI Chat Solution with .NET, Semantic Kernel, Python, and and Prompty  

## Overview

This project provides a guide to operationalizing a Generative AI (GenAI) solution on Azure using a Retrieval-Augmented Generation (RAG) architecture. It leverages Azure components like Azure AI Search for vector storage, Azure OpenAI for model inference, and integrates a .NET for API. Built with a focus on security, scalability, and user experience, this solution follows best practices for deploying RAG workflows on Azure.


## Key Features

- **RAG Architecture**: Implements a Retrieval-Augmented Generation workflow, enhancing GenAI capabilities with a vectorized retrieval layer for contextually relevant responses.
- **Azure  AI Search Vector Store**: Utilizes Azure AI Search for scalable and efficient storage of embeddings, supporting fast and reliable retrieval of data relevant to user queries.
- **Azure Cosmos DB Document Store**: Utilize Azure Cosmos DB to store and provide additional contect to the GenAI Chat application.
- **Azure OpenAI**: Integrates OpenAI models to enable high-quality generative responses and insights.
     - Implements the LangChain with Azure OpenAI Server.

- **.Net Backend**: Manages server-side processing, API orchestration, and integration with Azure services.
- **Event Driven Document Processing**: Use Azure Functions to create vector embeddings in Azure Cosmos DB for MongoDB VCore, triggered by document uploads.
- **Infrastructure as Code**: Uses Bicep and Azure CLI to deploy, manage, and scale resources consistently and securely across environments.

## Architecture
![diagram](./images/design1.png)

The architecture of this project includes:


- **Backend**: The backend is implemented in .Net, utilizing controller-based API for efficient handling of AI logic and data processing. Azure Functions are employed to manage event-driven functions, allowing for scalable and responsive execution as user interactions occur.
- **Database**: Azure Cosmos DB serves as the database solution, with Azure AI Search optimized for vector search capabilities. This enables fast and efficient retrieval of data, facilitating the application's AI functionalities.
- **AI Models**: The project incorporates the Azure OpenAI Service, which provides advanced generative text and embedding models. These models enhance the application’s ability to produce high-quality, contextually relevant responses based on user input.

## Requirements
- Azure subscription for deploying Azure GenAI RAG Application.
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/get-started-with-azure-cli) (Command Line Interface)
- .NET 8.0 is installed and properly configured in the development environment.
- Python 3.11.4 installed on development environment.
- An IDE for Development, such as [VS Code](https://code.visualstudio.com/download)


## Usage

Follow these steps to set up and deploy the solution:

### 1. Clone the Repository from GitHub:  
Begin by cloning the repository to your local machine using the following command:

```bash
git clone https://github.com/jonathanscholtes/Azure-AI-RAG-Architecture-React-FastAPI-and-Cosmos-DB-Vector-Store.git
cd Azure-AI-RAG-Architecture-React-FastAPI-and-Cosmos-DB-Vector-Store
```

### 2. Deploy the Solution Using Bicep:  
Navigate to the deployment directory:

```bash
cd infra
```

Then, use the following PowerShell command to deploy the solution. Make sure to replace the placeholders with your actual subscription name, Azure Region (location)

**PowerShell**
```bash
.\deploy.ps1 -Subscription '[Subscription Name]' -Location 'southcentralus'
```

This script will provision the necessary resources in your Azure subscription according to the specified parameters. The deployment may take upto **30 minutes** to provision all Azure resources.


### 3. Upload Documents for Azure AI Search Indexing:

Upload the HTML files from the [documents](documents) directory to the **load** container of the Azure Storage Account.

![storage network](./media/storage_container_load.png)

### 4. Validate Embedding Load in Azure CosmosDB for MongoDB vCore
To verify the successful loading of the documents, use [MongoDB Compass](https://www.mongodb.com/products/tools/compass) or a similar tool.

##### Configure Azure CosmosDB for MongoDB vCore Access:
To Validate with [MongoDB Compass](https://www.mongodb.com/products/tools/compass) you will need to add your client IP address to Azure CosmosDB for MongoDB vCore firewall settings.

- Log in to the Azure portal.
- Navigate to your Azure CosmosDB for MongoDB vCore.
- Under the **Networking** section, locate the **Public access** settings.
- Add your current client IP address to the list of allowed addresses.

##### Connect with MongoDB Compass:

Retreive connection string for Azure CosmosDB:
- Log in to the Azure portal.
- Navigate to your Azure CosmosDB for MongoDB vCore.
- Under the **Settings** section, select **Connections strings**
- Copy connection string for new connection with MongoDB Compass

Once connected you will see that 120 documents loaded into Azure CosmosDB for MongoDB vCore:
![compass](./images/mongodb_compass.png)


### 6. Validate GenAI Application

Access the Azure Web App hosting the React user interface (default name: web-vector-search-) by navigating to its assigned domain. Once the application loads, test its functionality by submitting the default question, "What is supersonic combustion?"

Upon submission, you should see the following results displayed:

- **Generated Response**: A detailed explanation or summary generated by the AI, outlining the principles of supersonic combustion.
- **Relevant Documents**: A list of contextually relevant documents retrieved from the Cosmos DB vector store, each contributing to the AI's response.

 This initial query confirms that the application is successfully retrieving, processing, and displaying data as designed, allowing you to explore additional questions or further customize the solution.

![results1](./images/react_web_results1.png)

Selecting the returned document pages will display the page images from the Azure Storage Account:

![results1](./images/react_web_results2.png)



## Clean-Up

After completing testing, ensure to delete any unused Azure resources or remove the entire Resource Group to avoid incurring additional charges.

### Resource Purge for Redeployment

If you plan to delete and redeploy this solution, you may need to manually purge specific Azure resources, including **Azure API Management (APIM)** and **Azure OpenAI** services, as they may retain certain configurations even after deletion.

#### Azure OpenAI Purge

To permanently delete an Azure OpenAI or other Azure AI service, follow [these steps](https://learn.microsoft.com/en-us/azure/ai-services/recover-purge-resources?tabs=azure-portal#purge-a-deleted-resource):

1. **Navigate to the Azure AI Service Hub**:  
   Go to the hub of the Azure AI service associated with the deleted resource. For instance, if you need to purge an "Anomaly Detector" resource, search for "Anomaly Detector" in the Azure portal search bar, select the service, and then choose **Manage Deleted Resources** from the menu.

2. **Select the Subscription and Deleted Resource**:  
   In the **Manage Deleted Resources** section, choose the relevant subscription from the dropdown list, then locate and select the deleted resource(s) you wish to purge.

3. **Purge the Resource**:  
   Click **Purge** to permanently delete the selected resource(s). Be aware that purging is irreversible and removes all associated configurations and data for the resource.

#### APIM Purge

To manually delete an Azure API Management (APIM) service, use the following command, substituting your **Subscription ID**, **Region**, and **APIM Resource Name**:

```bash
az rest --method delete --url https://management.azure.com/subscriptions/[Subscription ID]/providers/Microsoft.ApiManagement/locations/[Region]/deletedservices/[APIM Resource Name]?api-version=2021-08-01
```

## License
This project is licensed under the [MIT License](MIT.md), granting permission for commercial and non-commercial use with proper attribution.

## Support
For any questions or issues, please [open an issue](https://github.com/jonathanscholtes/Azure-AI-RAG-Architecture-React-FastAPI-and-Cosmos-DB-Vector-Store/issues) on GitHub or reach out to the project maintainers.

## Disclaimer
This demo application is intended solely for educational and demonstration purposes. It is provided "as-is" without any warranties, and users assume all responsibility for its use.
