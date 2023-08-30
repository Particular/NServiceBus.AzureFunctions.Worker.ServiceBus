# NServiceBus.AzureFunctions.Worker.ServiceBus

Process messages in AzureFunctions using the Azure Service Bus trigger and the NServiceBus message pipeline.

## How to test locally

Requirements:

- Have the [Microsoft Azurite Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite) installed and running before you run the tests.
- Configure an environment variable named `AzureWebJobsServiceBus` with an Azure Service Bus connection string
