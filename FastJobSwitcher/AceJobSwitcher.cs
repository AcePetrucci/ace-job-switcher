using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace AceJobSwitcher;

public class AceJobSwitcher : IDisposable
{
    private readonly ConfigurationMKII configuration;
    private List<ClassJob>? classJobSheet;
    private List<MKDSupportJob>? phantomJobSheet;
    private HashSet<string> registeredCommands = new();
    
    private static readonly Dictionary<string, string> PhantomJobNameAcronymMap = new()
    {
        { "Phantom Freelancer", "PFRE" },
        { "Phantom Knight", "PKNT" },
        { "Phantom Berserker", "PBER" },
        { "Phantom Monk", "PMNK" },
        { "Phantom Ranger", "PRNG" },
        { "Phantom Samurai", "PSAM" },
        { "Phantom Bard", "PBRD" },
        { "Phantom Geomancer", "PGEO" },
        { "Phantom Time Mage", "PTIM" },
        { "Phantom Cannoneer", "PCAN" },
        { "Phantom Chemist", "PCHM" },
        { "Phantom Oracle", "PORC" },
        { "Phantom Thief", "PTHF" },
    };

    public AceJobSwitcher(ConfigurationMKII configuration)
    {
        this.configuration = configuration;
        classJobSheet = Service.Data.Excel.GetSheet<ClassJob>()?.ToList();
        if (classJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load ClassJob sheet.");
        }

        phantomJobSheet = Service.Data.Excel.GetSheet<MKDSupportJob>()?.ToList();
        if (phantomJobSheet == null)
        {
            Service.PluginLog.Warning("Failed to load MKDSupportJob sheet.");
        }
        else
        {
            // print to log every row in phantomJobSheet
            foreach (var row in phantomJobSheet)
            {
                var strings = row.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(ReadOnlySeString))
                    .Select(p => (p.GetValue(row) as ReadOnlySeString?)?.ToString() ?? string.Empty)
                    .ToArray();
                Service.PluginLog.Information($"Phantom Job: {string.Join(", ", strings)}");
            }
        }

        Register();
    }

    public void Dispose()
    {
        UnRegister();
    }

    public void Register()
    {
        if (configuration.RegisterClassJobs)
        {
            classJobSheet?.ToList().ForEach(row =>
            {
                var acronym = row.Abbreviation.ToString();
                var name = row.Name.ToString();
                var rId = row.RowId;
                if (!string.IsNullOrWhiteSpace(acronym) && !string.IsNullOrWhiteSpace(name) && rId != 0)
                {
                    var suffixesToRegister = configuration.RegisterCommandSuffixes ? configuration.CommandSuffixes : new List<string> { "" };
                    
                    foreach (var suffix in suffixesToRegister)
                    {
                        var baseCommand = "/" + acronym;
                        var upperCommand = suffix == "" ? baseCommand.ToUpperInvariant() : baseCommand.ToUpperInvariant() + suffix.ToUpperInvariant();
                        var lowerCommand = suffix == "" ? baseCommand.ToLowerInvariant() : baseCommand.ToLowerInvariant() + suffix;
                        
                        RegisterCommand(upperCommand, name, "Class/Job");
                        RegisterCommand(lowerCommand, name, "Class/Job");
                    }
                }
            });
        }

        if (configuration.RegisterPhantomJobs)
        {
            phantomJobSheet?.ForEach(row =>
            {
                var jobName = row.Unknown0.ExtractText();
                var acronym = PhantomJobNameToAcronym(jobName);
                if (!string.IsNullOrWhiteSpace(acronym))
                {
                    var command = "/" + acronym;
                    RegisterCommand(command.ToUpperInvariant(), jobName, "Phantom Job");
                    RegisterCommand(command.ToLowerInvariant(), jobName, "Phantom Job");
                }
            });
        }
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

        // Handle class job commands (3 characters base + optional suffix)
        if (command.Length >= 3 && classJobSheet != null)
        {
            var baseCommand = command.Substring(0, 3);
            var cj = classJobSheet.FirstOrDefault(row => row.Abbreviation.ToString().Equals(baseCommand, StringComparison.InvariantCultureIgnoreCase));
            
            if (!cj.Equals(default(ClassJob)))
            {
                HandleClassJobCommand(cj, originalCommand);
                return;
            }
        }

        // Handle phantom job commands (4 characters)
        if (command.Length == 4 && phantomJobSheet != null)
        {
            HandlePhantomJobCommand(command);
        }
    }

    private void RegisterCommand(string command, string name, string type)
    {
        if (Service.Commands.Commands.ContainsKey(command))
        {
            Service.PluginLog.Warning($"Command already exists: {command}");
        }
        else
        {
            registeredCommands.Add(command);
            Service.Commands.AddHandler(command, new CommandInfo(OnCommand)
            {
                HelpMessage = $"Switches to {name} {type}",
                ShowInHelp = false,
            });
            Service.PluginLog.Information($"Registered command: {command} for {name} {type}");
        }
    }

    private unsafe void HandleClassJobCommand(ClassJob cj, string originalCommand)
    {
        if (cj.Equals(default(ClassJob)))
        {
            var msg = $"JobSwitch: No class job found for command: {originalCommand}";
            // Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
            return;
        }

        if (TryEquipBestGearsetForClassJob(cj, originalCommand))
        {
            Service.PluginLog.Information($"JobSwitch: Equipped best gearset for class job: {cj.Name}");
        }
        else
        {
            var msg = $"JobSwitch: No gearset found for class job: {cj.Name}";
            // Service.PluginLog.Error(msg);
            Service.ChatGui.PrintError(msg);
        }
    }

    private string PhantomJobNameToAcronym(string name)
    {
        return PhantomJobNameAcronymMap.TryGetValue(name, out var acronym) ? acronym : string.Empty;
    }

    private string PhantomJobAcronymToName(string acronym)
    {
        foreach (var kvp in PhantomJobNameAcronymMap)
        {
            if (string.Equals(kvp.Value, acronym, StringComparison.InvariantCultureIgnoreCase))
                return kvp.Key;
        }
        return string.Empty;
    }

    private unsafe void HandlePhantomJobCommand(string command)
    {
        if (command.StartsWith("p", StringComparison.InvariantCultureIgnoreCase))
        {
            var jobName = PhantomJobAcronymToName(command);

            if (string.IsNullOrWhiteSpace(jobName))
            {
                var msg = $"JobSwitch: No Phantom Job found for command: {command}";
                // Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var row = phantomJobSheet!.FirstOrDefault(row => row.Unknown0.ExtractText().Equals(jobName, StringComparison.InvariantCultureIgnoreCase));

            if (row.Equals(default(MKDSupportJob)))
            {
                var msg = $"JobSwitch: No Phantom Job found for command: {command}";
                // Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            if (GameMain.Instance()->CurrentTerritoryIntendedUseId != 61)
            {
                var msg = "You can only use this command in the Occult Crescent";
                // Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var jobId = row.RowId;

            var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.MKDSupportJobList);

            if (agent == null)
            {
                var msg = "Failed to get MKDSupportJobList agent.";
                Service.PluginLog.Error(msg);
                Service.ChatGui.PrintError(msg);
                return;
            }

            var eventObject = stackalloc AtkValue[1];
            var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
            atkValues[0].Type = ValueType.UInt;
            atkValues[0].UInt = 0;
            atkValues[1].Type = ValueType.UInt;
            atkValues[1].UInt = jobId;

            try
            {
                agent->ReceiveEvent(eventObject, atkValues, 2, 1);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Failed to switch Phantom Job: {ex.Message}");
                Service.ChatGui.PrintError($"Failed to switch Phantom Job: {ex.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
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

        // Remove leading slash and convert to lowercase for comparison
        var cleanCommand = commandText;
        if (cleanCommand.StartsWith("/"))
            cleanCommand = cleanCommand.Substring(1);
        
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