using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public class PluginLoader
{
    private static readonly List<Assembly> _loadedPlugins = new();

    public static async Task LoadAndExecutePluginsForAllGuildsAsync(DiscordSocketClient client, Func<ulong, Task<ulong>> getLogChannelIdAsync)
    {
        _loadedPlugins.Clear();

        var pluginFiles = Directory.GetFiles("plugins", "*.cs"); // Load all .cs files
        foreach (var file in pluginFiles)
        {
            try
            {
                Console.WriteLine($"Compiling plugin: {Path.GetFileName(file)}");

                var code = File.ReadAllText(file);
                var assembly = CompileCode(code);

                if (assembly != null)
                {
                    _loadedPlugins.Add(assembly);
                    Console.WriteLine($"✅ Loaded plugin: {Path.GetFileName(file)}");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to compile plugin: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading plugin {file}: {ex.Message}");
            }
        }

        // Initialize plugins for each guild
        var guildTasks = client.Guilds.Select(async guild =>
        {
            ulong logChannelId = await getLogChannelIdAsync(guild.Id);
            Console.WriteLine($"🔹 Initializing plugins for Guild: {guild.Name} ({guild.Id}) - LogChannel: {logChannelId}");
            await LoadAndExecutePluginsAsync(client, guild.Id, logChannelId);
        });

        await Task.WhenAll(guildTasks);
    }

    private static async Task LoadAndExecutePluginsAsync(DiscordSocketClient client, ulong guildId, ulong logChannelId)
    {
        var tasks = _loadedPlugins.Select(plugin => ExecutePluginSafelyAsync(plugin, client, guildId, logChannelId));
        await Task.WhenAll(tasks);
    }

    private static Assembly CompileCode(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create(
            $"Plugin_{Guid.NewGuid()}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            Console.WriteLine("⚠️ Compilation errors:");
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.WriteLine($"   → {diagnostic.GetMessage()}");
            }
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        return AssemblyLoadContext.Default.LoadFromStream(ms);
    }

    private static async Task ExecutePluginSafelyAsync(Assembly assembly, DiscordSocketClient client, ulong guildId, ulong logChannelId)
    {
        try
        {
            await ExecutePluginAsync(assembly, client, guildId, logChannelId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error executing plugin: {ex.Message}");
        }
    }

    private static async Task ExecutePluginAsync(Assembly assembly, DiscordSocketClient client, ulong guildId, ulong logChannelId)
    {
        foreach (var type in assembly.GetTypes())
        {
            var method = type.GetMethod("RunAsync");
            if (method == null) continue;

            var constructor = type.GetConstructor(new[] { typeof(DiscordSocketClient), typeof(ulong), typeof(ulong) })
                            ?? type.GetConstructor(new[] { typeof(DiscordSocketClient), typeof(ulong) });

            if (constructor == null)
            {
                Console.WriteLine($"⚠️ No valid constructor found for {type.Name}, skipping.");
                continue;
            }

            try
            {
                var parameters = constructor.GetParameters().Length == 3
                    ? new object[] { client, guildId, logChannelId }
                    : new object[] { client, guildId };

                var instance = constructor.Invoke(parameters);
                var task = method.Invoke(instance, null) as Task;
                if (task != null)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error running plugin {type.Name}: {ex.Message}");
            }
        }
    }
}
