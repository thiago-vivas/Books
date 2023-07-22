﻿// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus;

Console.WriteLine("Hello, World!");
string connectionString = "Endpoint=sb://thiagosample.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=e6Bb5GnhuadJfa1W49kCxwfBztioIAHXU+ASbH7kjrc=";
string queueName = "samplequeue";
string sessionQueueName = "samplesessionqueue";
string topicName = "sampletopic";
string subscriptionName = "samplesubscription";

// since ServiceBusClient implements IAsyncDisposable we create it with "await using"
ServiceBusClient client = new ServiceBusClient(connectionString);

// create the options to use for configuring the processor
var options = new ServiceBusSessionProcessorOptions
{
    // By default after the message handler returns, the processor will complete the message
    // If I want more fine-grained control over settlement, I can set this to false.
    AutoCompleteMessages = false,

    // I can also allow for processing multiple sessions
    MaxConcurrentSessions = 5,

    // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
    // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
    // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
    MaxConcurrentCallsPerSession = 2,

    // Processing can be optionally limited to a subset of session Ids.
    SessionIds = { "sampleSessionId", "sampleSessionId2" },
};

// create a session processor that we can use to process the messages
await using ServiceBusSessionProcessor processor = client.CreateSessionProcessor(sessionQueueName, options);

// configure the message and error handler to use
processor.ProcessMessageAsync += MessageHandler;
processor.ProcessErrorAsync += ErrorHandler;

async Task MessageHandler(ProcessSessionMessageEventArgs args)
{

    var body = args.Message.Body.ToString();

    Console.WriteLine("Session Id: " + args.Message.SessionId);
    Console.WriteLine("Body" + body);

    // we can evaluate application logic and use that to determine how to settle the message.
    await args.CompleteMessageAsync(args.Message);

    // we can also set arbitrary session state using this receiver
    // the state is specific to the session, and not any particular message
    await args.SetSessionStateAsync(new BinaryData("sample state"));
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    // the error source tells me at what point in the processing an error occurred
    Console.WriteLine(args.ErrorSource);
    // the fully qualified namespace is available
    Console.WriteLine(args.FullyQualifiedNamespace);
    // as well as the entity path
    Console.WriteLine(args.EntityPath);
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

// start processing
await processor.StartProcessingAsync();

// since the processing happens in the background, we add a Conole.ReadKey to allow the processing to continue until a key is pressed.
Console.ReadKey();

await client.DisposeAsync();
await processor.DisposeAsync();