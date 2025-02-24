using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class PluginLoader
{
    private static readonly List<Assembly> _loadedPlugins = new List<Assembly>();

    public static async Task LoadAndExecutePluginsAsync(DiscordSocketClient _client)
    {
        _loadedPlugins.Clear();

        var pluginFiles = Directory.GetFiles("plugins", "*.cs"); // Load all .cs files in the "plugins" directory
        foreach (var file in pluginFiles)
        {
            try
            {
                Console.WriteLine($"Loading plugin: {Path.GetFileName(file)}");

                var code = File.ReadAllText(file);
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies())
                    .WithImports(
                        "System",
                        "System.IO",
                        "System.Linq",
                        "System.Collections.Generic",
                        "Discord",
                        "Discord.WebSocket",
                        "Discord.Commands",
                        "System.Threading.Tasks"
                    );

                var script = CSharpScript.Create(code, scriptOptions);
                var compilation = script.GetCompilation();

                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    _loadedPlugins.Add(assembly);
                    Console.WriteLine($"Loaded plugin: {Path.GetFileName(file)}");
                }
                else
                {
                    Console.WriteLine($"Error compiling plugin {file}: {string.Join(", ", result.Diagnostics)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading plugin {file}: {ex.Message}");
            }
        }

        _ = Task.Run(async () =>
        {
            foreach (var plugin in _loadedPlugins)
            {
                await ExecutePluginAsync(plugin, _client);
            }
        });
    }

    private static async Task ExecutePluginAsync(Assembly assembly, DiscordSocketClient _client)
    {
        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetMethod("RunAsync") != null)
                {
                    var constructor = type.GetConstructor(new[] { typeof(DiscordSocketClient) });

                    if (constructor != null)
                    {
                        var instance = constructor.Invoke(new object[] { _client });
                        var method = type.GetMethod("RunAsync");

                        if (method != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    Console.WriteLine($"Executing plugin: {type.Name}");
                                    var task = method.Invoke(instance, null) as Task;

                                    if (task != null)
                                    {
                                        await task;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error executing plugin {type.Name}: {ex.Message}");
                                }
                            });
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No valid constructor found for {type.Name}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing plugin: {ex.Message}");
        }
    }
}
