﻿namespace NServiceBus.AzureFunctions.Worker.ServiceBus.Tests;

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus.Administration;
using NServiceBus.AzureFunctions.Worker.ServiceBus;
using NUnit.Framework;

[SetUpFixture]
public class OneTimeSetupAndTearDown
{
    [OneTimeSetUp]
    public async Task RunBeforeAllTests()
    {
        var connectionString = Environment.GetEnvironmentVariable(ServerlessTransport.DefaultServiceBusConnectionName);
        Assert.That(connectionString, Is.Not.Null, $"Environment variable '{ServerlessTransport.DefaultServiceBusConnectionName}' should be defined to run tests.");

        var client = new ServiceBusAdministrationClient(connectionString);

        const string errorQueueName = "error";

        if (!await client.QueueExistsAsync(errorQueueName))
        {
            await client.CreateQueueAsync(errorQueueName);
        }
    }
}