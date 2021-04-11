using CloudNimble.Breakdance.Assemblies;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tools
{


    class Program
    {
        public static async Task Main(string[] args)
        {

            var app = new CommandLineApplication()
            {
                //ExtendedHelpText = "Starting without a path to a CSX file or a command, starts the REPL (interactive) mode."
            };


            const string helpOptionTemplate = "-? | -h | --help";

            app.HelpOption(helpOptionTemplate);

            app.Command("generate", c =>
            {
                c.Description = "Execute manifest generators specified in your Test projects.";
                var root = c.Option("-path <path>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);
                var config = c.Option("-config <configuration>", "The desired project / solution configuration. Defaults to Debug.", CommandOptionType.SingleValue);
                var project = c.Option("-project <project>", "A single project in the specified path to analyze.", CommandOptionType.SingleValue);

                c.HelpOption(helpOptionTemplate);
                c.OnExecuteAsync(async cancellationToken =>
                {
                    var rootFolder = root.HasValue() ? root.Value() : Directory.GetCurrentDirectory();
                    var configuration = config.HasValue() ? config.Value() : "Debug";

                    await Generate(rootFolder, configuration, project.Value()).ConfigureAwait(false);

                    Console.WriteLine("Breakdance manifest generation has completed.");

                });

            });

            await app.ExecuteAsync(args).ConfigureAwait(false);

        }

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="config"></param>
        /// <param name="project"></param>
        private static async Task Generate(string path, string config, string project)
        {
            ColorConsole.WriteEmbeddedColorLine($"Looking for Tests in path [cyan]{path}[/cyan]...", ConsoleColor.Yellow);
            var projects = Directory.GetDirectories(path);
            //RWM: First find the test folders.
            var tests = projects.Where(c => !string.IsNullOrWhiteSpace(project) ? c.Contains(project) : c.ToLower().Contains(project ?? ".test")).OrderBy(c => c.Length);

            if (!tests.Any())
            {
                ColorConsole.WriteError($"No tests found.");
                return;
            }

            ColorConsole.WriteSuccess("The following Test projects were found:");
            foreach (var test in tests)
            {
                ColorConsole.WriteInfo($"{test}");
            }
            ColorConsole.WriteLine("");

            //RWM: Then find the tests using Breakdance
            foreach (var testPath in tests)
            {
                var fullTestPath = Path.Combine(testPath, "bin", config);
                ColorConsole.WriteEmbeddedColorLine($"Checking path [cyan]{fullTestPath}[/cyan] for Breakdance assemblies...", ConsoleColor.White);

                if (!Directory.Exists(fullTestPath))
                {
                    ColorConsole.WriteError("Directory does not exist. This may be because this Project may not be part of the Solution.\n");
                    continue;
                }

                var breakdanceDlls = Directory.GetFiles(fullTestPath, "CloudNimble.Breakdance.Assemblies.dll", SearchOption.AllDirectories);
                //RWM: Project doesn't use Breakdance, move on.
                if (breakdanceDlls.Length == 0)
                {
                    ColorConsole.WriteWarning("Breakdance not found at this location.\n");
                    continue;
                }

                ColorConsole.WriteSuccess("Breakdance assemblies found in the following folders:");
                foreach (var breakdanceDll in breakdanceDlls)
                {
                    ColorConsole.WriteInfo(Path.GetDirectoryName(breakdanceDll));
                }
                ColorConsole.WriteLine("");

                //RWM: Each path here represents a potentially different Target Framework. We only need one to work to complete the generation.
                //var generated = false;

                //RWM: Narrow down the assemblies we have to check
                foreach (var breakdanceDll in breakdanceDlls)
                {
                    var folderPath = Path.GetDirectoryName(breakdanceDll);
                    var testAssemblies = Directory.GetFiles(folderPath, "*.Test*.dll", SearchOption.TopDirectoryOnly).Where(c => !c.Contains("TestPlatform"));

                    if (!testAssemblies.Any())
                    {
                        ColorConsole.WriteWarning("Testable assemblies were not found at this location.\n");
                        continue;
                    }

                    foreach (var testAssembly in testAssemblies)
                    {
                        ColorConsole.WriteEmbeddedColorLine($"Checking for tests in [cyan]{Path.GetFileName(testAssembly)}[/cyan]", ConsoleColor.White);
                        Assembly assembly = null;
                        try
                        {
                            assembly = File.Exists(testAssembly) ? Assembly.LoadFrom(testAssembly) : Assembly.Load(testAssembly);
                        }
                        catch (FileLoadException ex)
                        {
                            ColorConsole.WriteError($"Assembly could not be loaded. {ex.Message}.");
                            continue;
                        }

                        if (assembly == null)
                        {
                            ColorConsole.WriteError("Assembly could not be loaded.");
                            continue;
                        }

                        var assemblyAttribute = assembly.GetCustomAttribute(typeof(BreakdanceTestAssemblyAttribute));
                        if (assemblyAttribute == null)
                        {
                            ColorConsole.WriteError("Assembly is not marked with the proper attribute. If this is the correct assembly, please make sure you add " +
                                "'<AssemblyAttribute Include=\"CloudNimble.Breakdance.Assemblies.BreakdanceTestAssembly\" />' to an ItemGroup in your .csproj file.");
                            continue;
                        }

                        var methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                              .Where(m => m.GetCustomAttributes(typeof(BreakdanceManifestGeneratorAttribute), false).Length > 0)
                              .ToList();

                        if (!methods.Any())
                        {
                            ColorConsole.WriteError("No suitable methods were found. Please make sure you have added '[BreakdanceManifestGenerator]' to the methods that " +
                                "write manifests to your project folders.");
                            continue;
                        }

                        foreach (var methodInfo in methods)
                        {
                            var parameterInfo = methodInfo.GetParameters();

                            switch (parameterInfo.Length)
                            {
                                case 0:
                                    ColorConsole.WriteWarning("Any relative path file writes will take place from the folder 'dotnet breakdance' was installed to. Consider adding a " +
                                        "'string path' parameter to the method so the tool can pass in the root path for the Test Project as a parameter.");

                                    await InvokeMethod(methodInfo, null).ConfigureAwait(false);
                                    break;
                                case 1:
                                    //RWM: No warning necessary.
                                    if (parameterInfo[0].ParameterType == typeof(string))
                                    {
                                        await InvokeMethod(methodInfo, new object[] { testPath }).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        ColorConsole.WriteError($"Method parameter is not a string. Change the parameter type, ore remove it, and try again.\nDon't forget to recompile.");
                                    }
                                    break;
                                default:
                                    ColorConsole.WriteError($"Method has too many parameters. Please specify a single string parameter representing the base path for writing files and try again.\nDon't forget to recompile.");
                                    break;
                            }
                            
                        }

                    }

                    ColorConsole.WriteLine("");

                }

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        private static async Task InvokeMethod(MethodInfo methodInfo, object[] parameters)
        {
            ColorConsole.WriteWarning($"Attempting to invoke method {methodInfo.DeclaringType.Name}.{methodInfo.Name}");
            try
            {

                var instance = Activator.CreateInstance(methodInfo.DeclaringType);
                //RWM: Handle the Async method case, it needs to be awaited to actually get the result written.
                //     Taken from https://stackoverflow.com/questions/20350397/how-can-i-tell-if-a-c-sharp-method-is-async-await-via-reflection
                if (methodInfo.GetCustomAttribute(typeof (AsyncStateMachineAttribute)) != null)
                {
                    await (dynamic)methodInfo.Invoke(instance, parameters);
                }
                else
                {
                    methodInfo.Invoke(instance, parameters);
                }
                ColorConsole.WriteSuccess("Method was invoked successfully.\n");
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError("The method could not be invoked. Make sure the only parameter is the a string that allows Breakdance to pass over the current project path");
                var exception = ex.Demystify();
                ColorConsole.WriteError($"Exception: {exception.Message}");
                ColorConsole.WriteError($"Stack Trace: {exception.StackTrace}");
            }
        }

        #endregion

    }

}
