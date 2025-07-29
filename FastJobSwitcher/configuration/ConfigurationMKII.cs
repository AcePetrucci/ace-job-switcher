using System;
using System.Collections.Generic;

namespace AceJobSwitcher;

[Serializable]
public class ConfigurationMKII : ConfigurationBase
{
    public override int Version { get; set; } = 1;

    public bool IsVisible { get; set; } = true;

    public bool RegisterClassJobs { get; set; } = true;

    public bool RegisterPhantomJobs { get; set; } = true;

    public bool RegisterCommandSuffixes { get; set; } = true;

    public List<string>? CommandSuffixes { get; set; }

    public List<string> GetCommandSuffixes()
    {
        if (CommandSuffixes == null || CommandSuffixes.Count == 0)
        {
            return GetDefaultSuffixes();
        }
        return CommandSuffixes;
    }

    private static List<string> GetDefaultSuffixes()
    {
        return
        [
            "", // Base command (no suffix)
            // Ultimates
            "ucob", "uwu", "tea", "dsr", "top", "fru",
            // Field Operations
            "eu", "bo", "oc"
        ];
    }

    public static ConfigurationMKII MigrateFrom(ConfigurationMKI oldConfig)
    {
        if (oldConfig == null)
        {
            return new ConfigurationMKII();
        }

        return new ConfigurationMKII
        {
            IsVisible = oldConfig.IsVisible,
            RegisterClassJobs = oldConfig.RegisterLowercaseCommands || oldConfig.RegisterUppercaseCommands,
        };
    }
}