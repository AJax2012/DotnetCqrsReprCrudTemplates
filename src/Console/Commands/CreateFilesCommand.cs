using System.Collections.Immutable;
using Cocona;
using DotnetCqrsClassTemplatesUtility.Console.Models;
using Humanizer;
using Spectre.Console;

namespace DotnetCqrsClassTemplatesUtility.Console.Commands;

public class CreateFilesCommand()
{
	private readonly Dictionary<string, ImmutableList<string>> _files = new()
	{
		{ "Endpoints", ["Create{0}", "Delete{0}", "Get{1}", "Get{0}ById", "Update{0}", "{0}Endpoints"] },
		{ "Examples", ["Create{0}RequestExample", "Get{1}ResponseExample", "Get{0}ByIdResponseExample", "Update{0}RequestExample"] },
		{ "Requests", ["Create{0}Request", "Get{1}Request", "Update{0}Request"] },
		{ "Responses", ["{0}Resource", "Get{1}Response", "Get{0}ByIdResponse", "Get{0}ByIdResponseFactory", "Get{1}ResponseFactory"] },
		{ "Validators", ["Create{0}RequestValidator", "Update{0}RequestValidator"] },
		{ "Commands/Create", ["Create{0}Command", "Create{0}CommandHandler"] },
		{ "Commands/Delete", ["Delete{0}Command", "Delete{0}CommandHandler"] },
		{ "Commands/Update", ["Update{0}Command", "Update{0}CommandHandler"] },
		{ "Queries/GetById", ["Get{0}ByIdQuery", "Get{0}ByIdQueryHandler", "{0}DetailsDto", "{0}DetailsFactory"] },
		{ "Queries/GetPaginated", ["Get{1}PaginatedQuery", "Get{1}PaginatedQueryHandler", "Paginated{1}Dto", "Paginated{1}DtoFactory", "Paginated{1}NextPageDto", "{0}ListItemDto"] },
		{ "Common", ["I{1}Repository"] },
		{ "Domain", ["{0}Error", "{0}"] },
		{ "Infrastructure", ["{1}Repository"] },
		{ "Test/Integration", ["Create{0}EndpointTest", "Delete{0}EndpointTest", "Get{1}EndpointTest", "Get{0}ByIdEndpointTest", "Update{0}EndpointTest"] },
		{ "Test/Unit/Application/Commands", ["Create{0}CommandHandlerTest", "Delete{0}CommandHandlerTest", "Update{0}CommandHandlerTest"] },
		{ "Test/Unit/Application/Queries", ["Get{0}ByIdQueryHandlerTest", "Get{1}PaginatedQueryHandlerTest"] },
		{ "Test/Unit/Domain", ["{0}Test"] },
	};

	public Task CreateFiles([Option("path", ['p'])] string path,[Option("dryRun", ['u'], Description = "Prints out what files would be auto-generated.")]bool dryRun = true)
	{
		var projectPath = Path.GetFullPath(path); //Path.Join(Assembly.GetExecutingAssembly().Location, path);
		
		ValidateProjectFiles(projectPath);
		var name = GetNameFromUser();
		
		if (!IsValidEndpointName(projectPath, name))
		{
			AnsiConsole.WriteLine("Exiting...");
			return Task.CompletedTask;
		}

		var integrationTestProjectName = Path.GetFileNameWithoutExtension(
			Directory.EnumerateFiles(Path.Join(projectPath, "test/Integration"), "*.csproj", SearchOption.AllDirectories)
				.First());

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
		var pluralizedName = name.Pluralize();
		
		GenerateFilesForPath(projectPath, Path.Join("src/Api/Endpoints", pluralizedName), new Template("Templates/Api/Endpoints", name, solutionName), "Endpoints");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", pluralizedName, "Requests"), new Template("Templates/Contracts/Requests", name, solutionName), "Requests");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", pluralizedName, "Responses"), new Template("Templates/Contracts/Responses", name, solutionName), "Responses");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", pluralizedName, "Examples"), new Template("Templates/Contracts/Examples", name, solutionName), "Examples");
		GenerateFilesForPath(projectPath, Path.Join("src/Contracts", pluralizedName, "Validators"), new Template("Templates/Contracts/Validators", name, solutionName), "Validators");
		GenerateFilesForPath(projectPath, Path.Join("src/Application", pluralizedName, "Commands/Create"), new Template("Templates/Application/Commands/Create", name, solutionName), "Commands/Create");
		GenerateFilesForPath(projectPath, Path.Join("src/Application", pluralizedName, "Commands/Delete"), new Template("Templates/Application/Commands/Delete", name, solutionName), "Commands/Delete");
		GenerateFilesForPath(projectPath, Path.Join("src/Application", pluralizedName, "Commands/Update"), new Template("Templates/Application/Commands/Update", name, solutionName), "Commands/Update");
		GenerateFilesForPath(projectPath, Path.Join("src/Application", pluralizedName, "Queries/GetById"), new Template("Templates/Application/Queries/GetById", name, solutionName), "Queries/GetById");
		GenerateFilesForPath(projectPath, Path.Join("src/Application", pluralizedName, "Queries/GetPaginated"), new Template("Templates/Application/Queries/GetPaginated", name, solutionName), "Queries/GetPaginated");
		GenerateFilesForPath(projectPath, "src/Application/Common/Contracts/Persistence", new Template("Templates/Application/Common/Contracts/Persistence", name, solutionName), "Common");
		GenerateFilesForPath(projectPath, Path.Join("src/Infrastructure/Persistence", pluralizedName), new Template("Templates/Infrastructure/Persistence", name, solutionName), "Infrastructure");
		GenerateFilesForPath(projectPath, Path.Join("src/Domain", $"{name}AggregateRoot"), new Template("Templates/Domain", name, solutionName), "Domain");
		GenerateFilesForPath(projectPath, Path.Join("test/unit/Application", pluralizedName, "Commands"), new Template("Templates/Test/Unit/Application/Commands", name, solutionName), "Test/Unit/Application/Commands");
		GenerateFilesForPath(projectPath, Path.Join("test/unit/Application", pluralizedName, "Queries"), new Template("Templates/Test/Unit/Application/Queries", name, solutionName), "Test/Unit/Application/Queries");		
		GenerateFilesForPath(projectPath, Path.Join("test/unit/Domain", $"{name}AggregateRoot"), new Template("Templates/Test/Unit/Domain", name, solutionName), "Test/Unit/Domain");
		
		var apiFactoryName = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(Path.Join(projectPath, "test/integration", integrationTestProjectName), "*ApiFactory.cs").First());
		GenerateFilesForPath(projectPath, Path.Join("test/integration", integrationTestProjectName, $"{name}Endpoints"), new Template("Templates/Test/Integration", name, solutionName, apiFactoryName), "Test/Integration");
	}

	private void GenerateFilesForPath(string projectPath, string path, Template template, string fileType)
	{
		Directory.CreateDirectory(Path.Join(projectPath, path));
		var templateFiles = _files[fileType]
			.Select(x => new TemplateFile(x, $"{string.Format(x, template.Name, template.Name.Pluralize())}.cs"))
			.ToImmutableList();

		foreach (var fileName in templateFiles)
		{
			var templatePath = Path.Join(template.TemplatePath, $"{fileName.TemplateName}.txt");
			var fileContents = File.ReadAllText(templatePath)
				.Replace("{{solutionName}}", template.SolutionName)
				.Replace("{{name}}", template.Name)
				.Replace("{{name|plural}}", template.Name.Pluralize())
				.Replace("{{name|lower}}", template.Name.ToLower())
				.Replace("{{name|lower|plural}}", template.Name.Pluralize().ToLower())
				.Replace("{{factoryName}}", template.ApiFactoryName ?? string.Empty);
			
			var filePath = Path.Join(projectPath, path, fileName.FileName);
			
			WriteNewFilePath(path);
			File.WriteAllText(filePath, fileContents);
		}
	}

	private ImmutableList<string> GetFileNames(string type, string name) =>
		_files[type].Select(x => $"{string.Format(x, name, name.Pluralize())}.cs").ToImmutableList();

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
		if (!Directory.Exists(path))
		{
			throw new DirectoryNotFoundException($"The directory ({path}) does not exist. Please make sure you are in the correct directory.");
		}
		
		if (!Directory.EnumerateFiles(path, "*.sln").Any())
		{
			throw new FileNotFoundException("The solution file does not exist. Please make sure you are in the correct directory.");
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

	private void WriteDryRunOutputs(string name, string integrationTestProjectName)
	{
		AnsiConsole.Write(new Rows(new Text("Dry run mode enabled. No files will be created.", new Style(Color.Gold3))));
		AnsiConsole.WriteLine();
		Thread.Sleep(1000);
		WriteFileTree(name, integrationTestProjectName);
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

		var applicationNode = srcNode.AddNode("[purple]Application[/]");
		
		var commandsNode = applicationNode.AddNode("[blue]Commands[/]");
		
		commandsNode.AddNode("[cyan1]Create[/]")
			.AddNodes(GetFileNames("Commands/Create", name));
		
		commandsNode.AddNode("[aquamarine1]Delete[/]")
			.AddNodes(GetFileNames("Commands/Delete", name));
		
		commandsNode.AddNode("[green1]Update[/]")
			.AddNodes(GetFileNames("Commands/Update", name));
		
		applicationNode.AddNode("[green]Common[/]")
			.AddNode("Contracts")
			.AddNode("Persistence")
			.AddNodes(GetFileNames("Common", name));
		
		var queriesNode = applicationNode.AddNode("[red]Queries[/]");
		
		queriesNode.AddNode("[cyan1]GetById[/]")
			.AddNodes(GetFileNames("Queries/GetById", name));
		
		queriesNode.AddNode("[aquamarine1]GetPaginated[/]")
			.AddNodes(GetFileNames("Queries/GetPaginated", name));
		
		var testsNode = fileTree.AddNode("[red]test[/]");
		
		testsNode.AddNode("[cyan1]integration[/]")
			.AddNode(integrationTestProjectName)
			.AddNode($"{name}Endpoints")
			.AddNodes(GetFileNames("Test/Integration", name));
		
		var unitTestsNode = testsNode.AddNode("[aquamarine1]unit[/]");
		
		var applicationUnitTestsNode = unitTestsNode.AddNode("[blue]Application[/]")
			.AddNode(name);
			
		applicationUnitTestsNode.AddNode("[purple]Commands[/]")
			.AddNodes(GetFileNames("Test/Unit/Application/Commands", name));
		
		applicationUnitTestsNode.AddNode("[cyan1]Queries[/]")
			.AddNodes(GetFileNames("Test/Unit/Application/Queries", name));
		
		unitTestsNode.AddNode("[red]Domain[/]")
			.AddNode($"{name}AggregateRoot")
			.AddNodes(GetFileNames("Test/Unit/Domain", name));
			
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
}