﻿using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace AceJobSwitcher;

public sealed class AceJobSwitcherPlugin : IDalamudPlugin
{
    public string Name => "Ace Job Switcher";

    private const string commandName = "/acejob";

    public IDalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public ConfigurationMKII Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public AceJobSwitcherUI Window { get; init; }

    public AceJobSwitcher Switcher { get; init; }

    public AceJobSwitcherPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager)
    {
        pluginInterface.Create<Service>();

        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        WindowSystem = new("AceJobSwitcherPlugin");

        Configuration = LoadConfiguration();
        Configuration.Initialize(SaveConfiguration);

        Window = new AceJobSwitcherUI(Configuration)
        {
            IsOpen = Configuration.IsVisible
        };

        WindowSystem.AddWindow(Window);

        CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens the configuration window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        Switcher = new AceJobSwitcher(Configuration);
    }

    public void Dispose()
    {
        Switcher.Dispose();

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

        CommandManager.RemoveHandler(commandName);

        WindowSystem.RemoveAllWindows();
    }

    private ConfigurationMKII LoadConfiguration()
    {
        JObject? baseConfig = null;
        if (File.Exists(PluginInterface.ConfigFile.FullName))
        {
            var configJson = File.ReadAllText(PluginInterface.ConfigFile.FullName);
            baseConfig = JObject.Parse(configJson);
        }

        if (baseConfig != null)
        {
            if ((int?)baseConfig["Version"] == 0)
            {
                var configmki = baseConfig.ToObject<ConfigurationMKI>();
                if (configmki != null)
                {
                    return ConfigurationMKII.MigrateFrom(configmki);
                }
            }
            if ((int?)baseConfig["Version"] == 1)
            {
                var configmkii = baseConfig.ToObject<ConfigurationMKII>();
                if (configmkii != null)
                {
                    return configmkii;
                }
            }
        }

        return new ConfigurationMKII();
    }

    public void SaveConfiguration()
    {
        var configJson = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
        File.WriteAllText(PluginInterface.ConfigFile.FullName, configJson);
        if (Switcher != null)
        {
            Switcher.UnRegister();
            Switcher.Register();
        }
    }

    private void SetVisible(bool isVisible)
    {
        Configuration.IsVisible = isVisible;
        Configuration.Save();

        Window.IsOpen = Configuration.IsVisible;
    }

    private void OnCommand(string command, string args)
    {
        SetVisible(!Configuration.IsVisible);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    private void DrawConfigUI()
    {
        SetVisible(!Configuration.IsVisible);
    }
}
