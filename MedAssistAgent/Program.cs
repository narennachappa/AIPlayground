using Azure.AI.Projects;
using Azure.Identity;
using MedAssistAgent.Configuration;
using MedAssistAgent.Services;
using MedAssistAgent.Tools;
using Microsoft.Agents.AI;
using OpenAI.Responses;

try
{
    var configuration = FoundryConfiguration.FromEnvironment();
    var tools = new MedicationTools(new InMemoryMedicationRepository(), Console.Out);
    var project = new AIProjectClient(configuration.ProjectEndpoint, new DefaultAzureCredential());

    // Model inference only: no persistent Foundry agent is created.
    // The upstream OpenAI Responses adapter is marked experimental by its SDK.
#pragma warning disable OPENAI001
    AIAgent agent = project.ProjectOpenAIClient
        .GetProjectResponsesClientForModel(configuration.Model)
        .AsAIAgent(name: "MedAssist", instructions: AgentInstructions.Text,
            tools: tools.CreateFunctions().ToList());
#pragma warning restore OPENAI001

    AgentSession session = await agent.CreateSessionAsync();
    using var shutdown = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
    Console.WriteLine("MedAssist — synthetic records only; no clinical advice. Type exit or press Ctrl+C to quit.");

    while (!shutdown.IsCancellationRequested)
    {
        Console.Write("You: ");
        string? question;
        try { question = await Console.In.ReadLineAsync(shutdown.Token); }
        catch (OperationCanceledException) { break; }
        if (question is null || question.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
        if (string.IsNullOrWhiteSpace(question)) continue;

        using var turn = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        turn.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            var response = await agent.RunAsync(question, session, cancellationToken: turn.Token);
            Console.WriteLine($"MedAssist: {response}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Request cancelled or timed out.");
            if (shutdown.IsCancellationRequested) break;
            session = await agent.CreateSessionAsync();
            Console.WriteLine("Started a new conversation after the interrupted turn.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Avoid dumping SDK responses, tokens, or user content into console logs.
            Console.Error.WriteLine($"Request failed ({ex.GetType().Name}). Check authentication, model, endpoint and network access; see README.");
            session = await agent.CreateSessionAsync();
            Console.WriteLine("Started a new conversation after the failed turn.");
        }
    }
    return 0;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
