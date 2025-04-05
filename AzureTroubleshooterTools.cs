using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.SelfHelp;
using Azure.ResourceManager.SelfHelp.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

[McpServerToolType]
public static class AzureTroubleshooterTools
{

    [McpServerTool, Description("Create a troubleshooter session")]
    public static async Task<string> CreateTroubleshooter([Description("Resource Uri of the azure resource")]string scope)
    {
        string solutionId = "e104dbdf-9e14-4c9f-bc78-21ac90382231";
        string troubleshooterName = Guid.NewGuid().ToString();
        var client = new ArmClient(new DefaultAzureCredential());
        var troubleshooterId = SelfHelpTroubleshooterResource.CreateResourceIdentifier(scope, troubleshooterName);
        var troubleshooter = client.GetSelfHelpTroubleshooterResource(troubleshooterId);

        var data = new SelfHelpTroubleshooterData
        {
            SolutionId = solutionId,
            Parameters = { ["ResourceURI"] = scope }
        };

        ArmOperation<SelfHelpTroubleshooterResource> lro = await troubleshooter.UpdateAsync(WaitUntil.Completed, data);
        return $"Troubleshooter created with ID: {lro.Value.Data.Id}";
    }

    [McpServerTool, Description("Get the current state of the troubleshooter")]
    public static async Task<string> GetTroubleshooterStep(string scope, string troubleshooterName)
    {
        var troubleshooter = await GetTroubleshooter(scope, troubleshooterName);
        var result = await troubleshooter.GetAsync();
        return JsonSerializer.Serialize(result.Value.Data, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Continue the troubleshooter with user response")]
    public static async Task<string> ContinueTroubleshooter(
        string scope,
        string troubleshooterName,
        string stepId,
        string questionId,
        [Description("Question Type can be 'TextInput', 'RadioButton', 'MultiSelect', 'MultiLineInfoBox', 'Dropdown' ")]string questionType,
        string response)
    {
        var troubleshooter = await GetTroubleshooter(scope, troubleshooterName);

        var content = new TroubleshooterContinueContent
        {
            StepId = stepId,
            Responses =
            {
                new TroubleshooterResult
                {
                    QuestionId = questionId,
                    QuestionType = new TroubleshooterQuestionType(questionType),
                    Response = response,
                }
            }
        };

        await troubleshooter.ContinueAsync(content);
        return "Troubleshooter continued successfully.";
    }

    [McpServerTool, Description("End the troubleshooter session")]
    public static async Task<string> EndTroubleshooter(string scope, string troubleshooterName)
    {
        var troubleshooter = await GetTroubleshooter(scope, troubleshooterName);
        await troubleshooter.EndAsync();
        return "Troubleshooter session ended.";
    }

    [McpServerTool, Description("Restart the troubleshooter session")]
    public static async Task<string> RestartTroubleshooter(string scope, string troubleshooterName)
    {
        var troubleshooter = await GetTroubleshooter(scope, troubleshooterName);
        RestartTroubleshooterResult result = await troubleshooter.RestartAsync();
        return $"Troubleshooter restarted. Result: {result}";
    }

    private static async Task<SelfHelpTroubleshooterResource> GetTroubleshooter(string scope, string troubleshooterName)
    {
        ArmClient client = new ArmClient(new DefaultAzureCredential());
        ResourceIdentifier troubleshooterId = SelfHelpTroubleshooterResource.CreateResourceIdentifier(scope, troubleshooterName);
        return client.GetSelfHelpTroubleshooterResource(troubleshooterId);
    }
}
