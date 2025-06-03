using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FastJobSwitcher;

public class FastJobSwitcher : IDisposable
{
    private readonly ConfigurationMKI configuration;
    private ExcelSheet<ClassJob>? classJobSheet;
    private HashSet<string> registeredCommands = [];

    public FastJobSwitcher(ConfigurationMKI configuration)
    {
        this.configuration = configuration;
        classJobSheet = Service.Data.Excel.GetSheet<ClassJob>();
        if (classJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load ClassJob sheet.");
            return;
        }

        Register();
    }

    public void Dispose()
    {
        UnRegister();
    }

    public void Register()
    {
        if (classJobSheet == null)
        {
            return;
        }

        classJobSheet.ToList().ForEach(row =>
        {
            var acronym = row.Abbreviation.ToString();
            var name = row.Name.ToString();
            var rId = row.RowId;
            if (!string.IsNullOrWhiteSpace(acronym) && !string.IsNullOrWhiteSpace(name) && rId != 0)
            {
                var lower = ("/" + configuration.Prefix + acronym + configuration.Suffix).ToLowerInvariant();
                var upper = ("/" + configuration.Prefix + acronym + configuration.Suffix).ToUpperInvariant();

                // Define command suffixes for different content types
                var commandSuffixes = new List<string>
                {
                    "", // Base command (no suffix)
                    // Ultimates
                    "ucob", "uwu", "tea", "dsr", "top", "fru",
                    // Field Operations
                    "eu", "bo", "oc"
                };

                foreach (var suffix in commandSuffixes)
                {
                    var lowerCmd = suffix == "" ? lower : lower + suffix;
                    var upperCmd = suffix == "" ? upper : upper + suffix.ToUpperInvariant();

                    if (configuration.RegisterUppercaseCommands)
                    {
                        if (Service.Commands.Commands.ContainsKey(upperCmd))
                        {
                            Service.PluginLog.Warning($"Command already exists: {upperCmd}");
                        }
                        else
                        {
                            registeredCommands.Add(upperCmd);
                            Service.Commands.AddHandler(upperCmd, new CommandInfo(OnCommand)
                            {
                                HelpMessage = $"Switches to {name} class/job.",
                                ShowInHelp = false,
                            });
                        }
                    }
                    if (configuration.RegisterLowercaseCommands)
                    {
                        if (Service.Commands.Commands.ContainsKey(lowerCmd))
                        {
                            Service.PluginLog.Warning($"Command already exists: {lowerCmd}");
                        }
                        else
                        {
                            registeredCommands.Add(lowerCmd);
                            Service.Commands.AddHandler(lowerCmd, new CommandInfo(OnCommand)
                            {
                                HelpMessage = $"Switches to {name} class/job.",
                                ShowInHelp = false,
                            });
                        }
                    }
                }
            }
        });
    }

    public void UnRegister()
    {
        registeredCommands.ToList().ForEach(command =>
        {
            if (Service.Commands.Commands.ContainsKey(command))
            {
                Service.Commands.RemoveHandler(command);
            }
        });
    }

    protected void OnCommand(string command, string arguments)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }
        else if (command.StartsWith("/"))
        {
            command = command.Substring(1);
        }

        var originalCommand = command;
        command = command.Substring(0, 3);

        var cj = classJobSheet!.ToList().FirstOrDefault(row => row.Abbreviation.ToString().Equals(command, StringComparison.InvariantCultureIgnoreCase));

        if (cj.Equals(default(ClassJob)))
        {
            var msg = $"JobSwitch: No class job found for command: {command}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        var success = TryEquipBestGearsetForClassJob(cj, originalCommand);

        if (!success)
        {
            var msg = $"JobSwitch: No gearset found for class job: {cj.Name}";
            Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }
    }

    private unsafe bool TryEquipBestGearsetForClassJob(ClassJob cj, string commandText)
    {
        var rapture = RaptureGearsetModule.Instance();
        if (rapture != null)
        {
            byte? bestId = null;
            int bestScore = -1;
            
            for (var i = 0; i < 100; i++)
            {
                var gearset = rapture->GetGearset(i);
                if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) && gearset->Id == i && gearset->ClassJob == cj.RowId)
                {
                    var gearsetName = gearset->NameString;
                    var score = CalculateMatchScore(commandText, gearsetName);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestId = gearset->Id;
                    }
                }
            }
            
            if (bestId.HasValue)
            {
                rapture->EquipGearset(bestId.Value);
                return true;
            }
        }

        return false;
    }

    private int CalculateMatchScore(string commandText, string gearsetName)
    {
        if (string.IsNullOrWhiteSpace(commandText) || string.IsNullOrWhiteSpace(gearsetName))
            return 0;

        // Remove prefix and suffix from command for comparison
        var cleanCommand = commandText;
        if (cleanCommand.StartsWith("/"))
            cleanCommand = cleanCommand.Substring(1);
        if (cleanCommand.StartsWith(configuration.Prefix))
            cleanCommand = cleanCommand.Substring(configuration.Prefix.Length);
        if (cleanCommand.EndsWith(configuration.Suffix))
            cleanCommand = cleanCommand.Substring(0, cleanCommand.Length - configuration.Suffix.Length);

        // Convert both to lowercase for comparison
        cleanCommand = cleanCommand.ToLowerInvariant();
        var lowerGearsetName = gearsetName.ToLowerInvariant();

        // Exact match gets highest priority
        if (cleanCommand == lowerGearsetName)
            return 1000;

        // Check if gearset name starts with the command
        if (lowerGearsetName.StartsWith(cleanCommand))
            return 500 + cleanCommand.Length;

        // Check if gearset name contains the command
        if (lowerGearsetName.Contains(cleanCommand))
            return 250 + cleanCommand.Length;

        // Calculate character overlap
        int overlap = 0;
        int commandIndex = 0;
        for (int i = 0; i < lowerGearsetName.Length && commandIndex < cleanCommand.Length; i++)
        {
            if (lowerGearsetName[i] == cleanCommand[commandIndex])
            {
                overlap++;
                commandIndex++;
            }
        }

        // Return overlap score if we matched all command characters
        if (commandIndex == cleanCommand.Length)
            return overlap;

        return 0;
    }
}
