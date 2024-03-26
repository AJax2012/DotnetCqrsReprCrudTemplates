//-----------------------------------------------------------------------
// <copyright file="CreateEndpoints.cs" company="GardnerWebTech">
//    Copyright (c) GardnerWebTech. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Immutable;
using Cocona;
using DotnetCqrsClassTemplatesUtility.Console.Models;
using Humanizer;
using Spectre.Console;

namespace DotnetCqrsClassTemplatesUtility.Console.Commands;

public class CreateEndpoints()
{
	private readonly Dictionary<string, ImmutableList<string>> _files = new()
	{
		{ "Endpoints", ["Create{0}", "Delete{0}", "Get{0}s", "Get{0}ById", "Update{0}", "{0}Endpoints"] },
		{ "Examples", ["Create{0}RequestExample", "Get{0}sResponseExample", "Get{0}ByIdResponseExample", "Update{0}RequestExample"] },
		{ "Requests", ["Create{0}Request", "Update{0}Request"] },
		{ "Responses", ["Get{0}sResponse", "Get{0}ByIdResponse"] },
		{ "Validators", ["Create{0}RequestValidator", "Update{0}RequestValidator"] },
		{ "Tests", ["Create{0}EndpointTest", "Delete{0}EndpointTest", "Get{0}sEndpointTest", "Get{0}ByIdEndpointTest", "Update{0}EndpointTest"] },
	};

	public Task Testing([Option("dryRun", ['u'], Description = "Prints out what files would be auto-generated.")]bool dryRun = true)
	{
		// TODO: Delete tempProjectPath and replace with actual path.
		var projectPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Developer/TestProject");
		
		ValidateProjectFiles(projectPath);
		var name = GetNameFromUser();
		
		if (!IsValidEndpointName(projectPath, name))
		{
			AnsiConsole.WriteLine("Exiting...");
			return Task.CompletedTask;
		}

		var integrationTestProjectName = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(Path.Join(projectPath, "test/Integration"), "*.csproj", SearchOption.AllDirectories).First());

		if (dryRun)
		{
			WriteDryRunOutputs(name, integrationTestProjectName);

			var confirm = AnsiConsole.Prompt(new ConfirmationPrompt("Would you like to proceed with the file generation?").ShowChoices());

			if (!confirm)
			{
				AnsiConsole.WriteLine("Exiting...");
				return Task.CompletedTask;
			}
		}
		
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Text("Generating files...", new Style(Color.Green)));
		AnsiConsole.WriteLine();

		var solutionName = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(projectPath, "*.sln").First());
		
		GenerateFiles(projectPath, name, solutionName, integrationTestProjectName);

		AnsiConsole.Write(new Text("Files generated successfully.", new Style(Color.Green)));

		if (!dryRun)
		{
			WriteFileTree(name, integrationTestProjectName);
		}
		
		return Task.CompletedTask;
	}

	private void GenerateFiles(
		string projectPath,
		string name,
		string solutionName,
		string integrationTestProjectName)
	{
		GenerateFilesForPath(projectPath, Path.Join("src/Api/Endpoints", name), new Template("Templates/Api/Endpoints", name, solutionName), "Endpoints");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", name, "Requests"), new Template("Templates/Contracts/Requests", name, solutionName), "Requests");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", name, "Responses"), new Template("Templates/Contracts/Responses", name, solutionName), "Responses");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", name, "Examples"), new Template("Templates/Contracts/Examples", name, solutionName), "Examples");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", name, "Validators"), new Template("Templates/Contracts/Validators", name, solutionName), "Validators");
		
		var apiFactoryName = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(Path.Join(projectPath, "test/integration", integrationTestProjectName), "*ApiFactory.cs").First());
		GenerateFilesForPath(projectPath, Path.Join("test/integration", integrationTestProjectName, $"{name}Endpoints"), new Template("Templates/Test/Integration", name, solutionName, apiFactoryName), "Tests");
	}

	private void GenerateFilesForPath(string projectPath, string path, Template template, string fileType)
	{
		Directory.CreateDirectory(Path.Join(projectPath, path));
		var templateFiles = _files[fileType]
			.Select(x => new TemplateFile(x, $"{string.Format(x, template.Name)}.cs"))
			.ToImmutableList();

		foreach (var fileName in templateFiles)
		{
			var templatePath = Path.Join(template.TemplatePath, $"{fileName.TemplateName}.txt");
			var fileContents = File.ReadAllText(templatePath)
				.Replace("{{solutionName}}", template.SolutionName)
				.Replace("{{name}}", template.Name)
				.Replace("{{name|lower}}", template.Name.ToLower())
				.Replace("{{factoryName}}", template.ApiFactoryName ?? string.Empty);
			
			var filePath = Path.Join(projectPath, path, fileName.FileName);
			
			WriteNewFilePath(path);
			File.WriteAllText(filePath, fileContents);
		}
	}

	private ImmutableList<string> GetFileNames(string type, string name) =>
		_files[type].Select(x => $"{string.Format(x, name)}.cs").ToImmutableList();

	private static string GetNameFromUser()
	{

		var name = AnsiConsole.Prompt(
			new TextPrompt<string>("What would you like to name your new endpoint group?")
				.PromptStyle("green")
				.Validate(name => string.IsNullOrWhiteSpace(name) ? 
					ValidationResult.Error("Please enter a valid name.") : 
					ValidationResult.Success())
				.WithConverter(x => x.Trim().Dehumanize()));
		return name;
	}

	private static bool IsValidEndpointName(string projectPath, string name)
	{
		if (Directory.Exists(Path.Join(projectPath, "src/Api/Endpoints", name)))
		{
			return AnsiConsole.Prompt(new ConfirmationPrompt("[gold3]This endpoint group already exists. Would you like to overwrite it?[/]").ShowChoices());
		}

		return true;
	}

	private static void ValidateProjectFiles(string path)
	{
		if (!Directory.EnumerateFiles(path, "*.sln").Any())
		{
			throw new DirectoryNotFoundException("The solution file does not exist. Please make sure you are in the correct directory.");
		}
		
		var apiPath = Path.Join(path, "src/Api");
		
		if (!Directory.EnumerateFiles(apiPath, "*.csproj").Any())
		{
			throw new DirectoryNotFoundException("The src/Api project does not exist. Please make sure you are in the correct directory.");
		}
		
		var contractsPath = Path.Join(path, "src/Contracts");
		
		if(!Directory.EnumerateFiles(contractsPath, "*.csproj").Any())
		{
			throw new DirectoryNotFoundException("The src/Contracts project does not exist. Please make sure you are in the correct directory.");
		}
		
		var integrationTestPath = Path.Join(path, "test/integration");
		
		if (!Directory.EnumerateFiles(integrationTestPath, "*.csproj", SearchOption.AllDirectories).Any())
		{
			throw new DirectoryNotFoundException("The test/Integration project does not exist. Please make sure you are in the correct directory.");
		}
	}

	private void WriteDryRunOutputs(string name, string integrationTestDirectory)
	{
		AnsiConsole.Write(new Rows(new Text("Dry run mode enabled. No files will be created.", new Style(Color.Gold3))));
		AnsiConsole.WriteLine();
		Thread.Sleep(1000);
		WriteFileTree(name, integrationTestDirectory);
	}

	private void WriteFileTree(string name, string integrationTestProjectName)
	{
		var fileTree = new Tree("[aqua]sln[/]");
			
		var srcNode = fileTree.AddNode("[navy]src[/]");

		srcNode.AddNode("[green]Api[/]")
			.AddNode("Endpoints")
			.AddNode(name)
			.AddNodes(GetFileNames("Endpoints", name));
			
		var contractsNode = srcNode.AddNode("[orange1]Contracts[/]")
			.AddNode(name);
			
		contractsNode.AddNode("[blue]Examples[/]")
			.AddNodes(GetFileNames("Examples", name));
			
		contractsNode.AddNode("[cyan1]Requests[/]")
			.AddNodes(GetFileNames("Requests", name));
			
		contractsNode.AddNode("[aquamarine1]Responses[/]")
			.AddNodes(GetFileNames("Responses", name));
			
		contractsNode.AddNode("[green1]Validators[/]")
			.AddNodes(GetFileNames("Validators", name));

		fileTree.AddNode("[purple]test[/]")
			.AddNode("integration")
			.AddNode(integrationTestProjectName)
			.AddNode($"{name}Endpoints")
			.AddNodes(GetFileNames("Tests", name));
			
		AnsiConsole.Write(fileTree);
		AnsiConsole.WriteLine();
	}
	
	private static void WriteNewFilePath(string path)
	{
		AnsiConsole.Write(new Padder(new TextPath(path))
			.PadLeft(2)
			.PadTop(0)
			.PadBottom(0));
	}

	private record TemplateFile(string TemplateName, string FileName);
}