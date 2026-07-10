// Install NuGet.CommandLine as a Cake Tool
#tool nuget:?package=NuGet.CommandLine&version=6.2.1
#addin nuget:?package=Cake.FileHelpers&version=5.0.0
const string xunitRunnerVersion = "2.4.1";
#tool nuget:?package=xunit.runner.console&version=2.4.1
#tool nuget:?package=OpenCover&version=4.7.1221
#tool nuget:?package=7-Zip.CommandLine&version=18.1.0
#addin nuget:?package=Cake.7zip&version=2.0.0
#tool nuget:?package=ReportGenerator&version=5.1.9

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "DEBUG");

//from cake source:
/// <summary>
/// MSBuild tool version: <c>Visual Studio 2019</c>
/// </summary>
//VS2019 = 7,
/// <summary>
/// MSBuild tool version: <c>Visual Studio 2022</c>
/// </summary>
//VS2022 = 10

var msbuildVersion = Argument<int>("msbuildVersion", 7);
var useBinaryLog = Argument<bool>("useBinaryLog", false);
var isWorkflowRun = Argument<bool>("isWorkflowRun", false);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var rootAbsoluteDir = MakeAbsolute(Directory("./")).FullPath;
var logDirectory = MakeAbsolute(Directory($"./logs/{configuration}")).FullPath;
var outDirectory = MakeAbsolute(Directory($"./out/{configuration}")).FullPath;
var reportDirectory = MakeAbsolute(Directory($"./reports/{configuration}")).FullPath;
var openCoverDirectory = MakeAbsolute(Directory($"{reportDirectory}/OpenCover")).FullPath;
var reportGeneratorDirectory = MakeAbsolute(Directory($"{reportDirectory}/ReportGenerator")).FullPath;
var reportGeneratorHistoryDirectory = MakeAbsolute(Directory($"{reportDirectory}/History")).FullPath;

var solutionFiles = new List<string>
{
    "./../AnnoDesigner.sln",
    // ponytail: ColorPresetsDesigner, FandomParser, FandomTemplateExporter are separate tools
    // with their own solution files. They share the root Directory.Packages.props but some
    // packages need version alignment. Build them separately if needed.
    // "./../ColorPresetsDesigner.sln",
    // "./../FandomParser/FandomParser.sln",
    // "./../FandomTemplateExporter/FandomTemplateExporter.sln"
};

var versionNumber = System.IO.File.ReadAllText("./../version.txt");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
    Information("");
    Information($"{nameof(target)}: {target}");
    Information($"{nameof(configuration)}: {configuration}");
    Information("");
    Information($"{nameof(logDirectory)}: {logDirectory}");
    Information($"{nameof(outDirectory)}: {outDirectory}");
    Information($"{nameof(reportDirectory)}: {reportDirectory}");
    Information("");
    Information($"version: {versionNumber}");

    EnsureDirectoryExists(logDirectory);
    CleanDirectory(logDirectory);

    EnsureDirectoryExists(outDirectory);
    CleanDirectory(outDirectory);

    EnsureDirectoryExists(openCoverDirectory);
    CleanDirectory(openCoverDirectory);

    EnsureDirectoryExists(reportGeneratorDirectory);
    //keep history of code coverage
    //CleanDirectory(reportGeneratorDirectory);

    EnsureDirectoryExists(reportGeneratorHistoryDirectory);
});

Teardown(ctx =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

var cleanTask = Task("Clean")
.Description("Cleans all directories that are used during the build process.")
.Does(() =>
{
    CleanDirectories($"**/bin/{configuration}");
    CleanDirectories($"**/obj/{configuration}");
    CleanDirectories($"**/bin/*/{configuration}");
    CleanDirectories($"**/obj/*/{configuration}");
});

var restoreNuGetTask = Task("Restore-NuGet-Packages")
.Description("Restores all the NuGet packages that are used by the specified solution.")
.IsDependentOn(cleanTask)
.Does(() =>
{
    var settings = new NuGetRestoreSettings
    {
        Verbosity= NuGetVerbosity.Quiet,//NuGetVerbosity.Normal,
        NoCache = false
    };

    foreach (var curSolutionFile in solutionFiles)
    {
        var curSolutionFileName = System.IO.Path.GetFileName(curSolutionFile);
        Information($"{DateTime.Now:hh:mm:ss.ff} restoring NuGet packages for {curSolutionFileName}");
        NuGetRestore(curSolutionFile, settings);
    }
});

var updateAssemblyInfoTask = Task("Update-Assembly-Info")
.IsDependentOn(restoreNuGetTask)
.Does(() =>
{
    ReplaceRegexInFiles("./../AnnoDesigner/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");
    ReplaceRegexInFiles("./../AnnoDesigner/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyFileVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");

    ReplaceRegexInFiles("./../AnnoDesigner.Core/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");
    ReplaceRegexInFiles("./../AnnoDesigner.Core/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyFileVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");

    ReplaceRegexInFiles("./../PresetParser/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");
    ReplaceRegexInFiles("./../PresetParser/Properties/AssemblyInfo.cs",
                        "(?<=AssemblyFileVersion\\(\")(.+?)(?=\"\\))",
                        $"{versionNumber}.0.0");

    ReplaceRegexInFiles("./../AnnoDesigner/Constants.cs",
                        "(?<= new Version\\()(.+?)(?=\\);)",
                        $"{versionNumber}");
    //Replace dot (.) with comma (,)
    ReplaceRegexInFiles("./../AnnoDesigner/Constants.cs",
                        "(?<=new Version\\([1-9]{1})([.])(?=[0-9]+\\);)",
                        ", ");
});

var buildTask = Task("Build")
.Description("Builds all the different parts of the project.")
.IsDependentOn(cleanTask)
.Does(() =>
{
    foreach (var curSolutionFile in solutionFiles)
    {
        var curSolutionFileName = System.IO.Path.GetFileName(curSolutionFile);

        Information($"{DateTime.Now:hh:mm:ss.ff} compiling {curSolutionFileName}");

        // ponytail: replaced MSBuild() with DotNetBuild().
        // VS 2022 17.14 doesn't support .NET 10. dotnet build uses the SDK directly.
        var buildSettings = new DotNetBuildSettings
        {
            Configuration = configuration,
            Verbosity = DotNetVerbosity.Minimal,
            NoLogo = true
        };

        DotNetBuild(curSolutionFile, buildSettings);
        Information("");
    }
});

var runUnitTestsTask = Task("Run-Unit-Tests")
.IsDependentOn(buildTask)
    .Does(() =>
{
   var testAssemblies = GetFiles($"./../**/bin/**/{configuration}/**/*.Tests.dll");

    Information($"found {testAssemblies.Count} test assemblies:");
    foreach (var curTestAssembly in testAssemblies)
    {
        Information(curTestAssembly);
    }

    Information("");

    // ponytail: replaced OpenCover + xunit.console.exe (net472) with dotnet test.
    // OpenCover can't load .NET 10 assemblies. Use built-in coverage collection instead.
    // Upgrade path: add --collect:"XPlat Code Coverage" for coverage reports.
    var testSettings = new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Verbosity = DotNetVerbosity.Normal,
        ResultsDirectory = logDirectory
    };

    var testProjects = GetFiles("./../Tests/**/*.csproj");
    foreach (var project in testProjects)
    {
        Information($"{DateTime.Now:hh:mm:ss.ff} running tests for {project.GetFilename()}");
        DotNetTest(project.FullPath, testSettings);
    }
});

var copyFilesTask = Task("Copy-Files")
.IsDependentOn(buildTask)
.Does(() =>
{
    // ponytail: publish as self-contained for distribution.
    // The old approach copied individual DLLs from net48 output.
    // .NET 10 apps use `dotnet publish` for proper output.
    var publishDir = $"./../AnnoDesigner/bin/{configuration}/net10.0-windows8.0/publish";

    Information($"{DateTime.Now:hh:mm:ss.ff} publishing AnnoDesigner...");
    DotNetPublish("./../AnnoDesigner/AnnoDesigner.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDir,
        NoBuild = true
    });

    var outputDirectoryIcons = $"{outDirectory}/icons";
    EnsureDirectoryExists(outputDirectoryIcons);
    EnsureDirectoryExists(outDirectory);

    // Copy game icons
    var gameIconsDir = $"{publishDir}/Assets/game_icons";
    if (DirectoryExists(gameIconsDir))
    {
        Information($"{DateTime.Now:hh:mm:ss.ff} copy icons to \"{outputDirectoryIcons}\"");
        CopyDirectory(gameIconsDir, outputDirectoryIcons);
    }
    // Copy published output to out directory
    Information($"{DateTime.Now:hh:mm:ss.ff} copying published files to \"{outDirectory}\"");
    CopyDirectory(publishDir, outDirectory);

    Information("");
});

var zipTask = Task("Compress-Output")
.IsDependentOn(copyFilesTask)
.Does(() =>
{  
    Information($"{DateTime.Now:hh:mm:ss.ff} start of task");
    
    if(configuration.Equals("DEBUG", StringComparison.OrdinalIgnoreCase))// || isWorkflowRun)
    {
        Information($"{DateTime.Now:hh:mm:ss.ff} return from task");
        return;
    }

    var outputFilePath = $"{outDirectory}/../Anno.Designer.v{versionNumber}.zip";

    Information($"{DateTime.Now:hh:mm:ss.ff} creating zip file: \"{outputFilePath}\"");
    
    //use build in zip functionality (ignores file attributes)
    //Zip($"{outDirectory}", outputFilePath);
    
    //use 7zip tool (respects file attributes)
    SevenZip(m => m
      .InAddMode()
      .WithArchive(outputFilePath)
      //.WithArchiveType(SwitchArchiveType.SevenZip)
      .WithArchiveType(SwitchArchiveType.Zip)
      .WithCompressionMethodLevel(9)//seems to be the highest value = best compression
      .WithDirectoryContents(Directory(outDirectory)));

    Information("");
});

Task("Default")
.Description("This is the default task which will be ran if no specific target is passed in.")
//.IsDependentOn(copyFilesTask)
//.IsDependentOn(buildTask)
.IsDependentOn(runUnitTestsTask)
.IsDependentOn(zipTask)
.Does(() => {});

RunTarget(target);
