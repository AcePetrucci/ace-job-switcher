using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace AceJobSwitcher;

public class AceJobSwitcherUI : Window, IDisposable
{
    private readonly ConfigurationMKII configuration;
    private string newSuffix = "";

    public AceJobSwitcherUI(ConfigurationMKII configuration)
      : base(
        "Ace Job Switcher##ConfigWindow",
        ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoCollapse
      )
    {
        this.configuration = configuration;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(268, 0),
            MaximumSize = new Vector2(500, 1000)
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        configuration.IsVisible = false;
        configuration.Save();
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Register:");
        ImGui.Indent();
        {
            var classJobEnabled = configuration.RegisterClassJobs;
            if (ImGui.Checkbox("Classes and Jobs##ClassJobs", ref classJobEnabled))
            {
                configuration.RegisterClassJobs = classJobEnabled;
                configuration.Save();
            }

            var phantomJobsEnabled = configuration.RegisterPhantomJobs;
            if (ImGui.Checkbox("Phantom Jobs##PhantomJobs", ref phantomJobsEnabled))
            {
                configuration.RegisterPhantomJobs = phantomJobsEnabled;
                configuration.Save();
            }

            var commandSuffixesEnabled = configuration.RegisterCommandSuffixes;
            if (ImGui.Checkbox("Command Suffixes for Class Jobs##CommandSuffixes", ref commandSuffixesEnabled))
            {
                configuration.RegisterCommandSuffixes = commandSuffixesEnabled;
                configuration.Save();
            }
        }
        ImGui.Unindent();

        if (configuration.RegisterCommandSuffixes)
        {
            ImGui.NewLine();
            ImGui.TextWrapped("Command Suffixes:");
            ImGui.Text("Configure additional command variants (e.g., /blmucob, /blmtea)");
            
            ImGui.Indent();
            
            // Ensure we have a list to work with
            if (configuration.CommandSuffixes == null)
            {
                configuration.CommandSuffixes = configuration.GetCommandSuffixes();
                configuration.Save();
            }
            
            // Display existing suffixes with remove buttons
            for (int i = 0; i < configuration.CommandSuffixes.Count; i++)
            {
                var suffix = configuration.CommandSuffixes[i];
                var displayText = string.IsNullOrEmpty(suffix) ? "(base command)" : suffix;
                
                ImGui.PushID(i);
                
                // Text input for editing
                var editableSuffix = suffix;
                ImGui.SetNextItemWidth(150);
                if (ImGui.InputText($"##suffix{i}", ref editableSuffix, 32))
                {
                    configuration.CommandSuffixes[i] = editableSuffix;
                    configuration.Save();
                }
                
                ImGui.SameLine();
                
                // Remove button (but don't allow removing the base command at index 0)
                if (i == 0)
                {
                    ImGui.TextDisabled("(base)");
                }
                else if (ImGui.Button($"Remove##remove{i}"))
                {
                    configuration.CommandSuffixes.RemoveAt(i);
                    configuration.Save();
                    i--; // Adjust index since we removed an item
                }
                
                ImGui.PopID();
            }
            
            ImGui.NewLine();
            
            // Add new suffix
            ImGui.Text("Add new suffix:");
            ImGui.SetNextItemWidth(150);
            ImGui.InputText("##newSuffix", ref newSuffix, 32);
            ImGui.SameLine();
            if (ImGui.Button("Add") && !string.IsNullOrWhiteSpace(newSuffix))
            {
                // Check if suffix already exists
                if (!configuration.CommandSuffixes.Contains(newSuffix))
                {
                    configuration.CommandSuffixes.Add(newSuffix);
                    configuration.Save();
                    newSuffix = "";
                }
            }
            
            ImGui.NewLine();
            
            // Reset to defaults button
            if (ImGui.Button("Reset to Defaults"))
            {
                configuration.CommandSuffixes = new()
                {
                    "", // Base command (no suffix)
                    // Ultimates
                    "ucob", "uwu", "tea", "dsr", "top", "fru",
                    // Field Operations
                    "eu", "bo", "oc"
                };
                configuration.Save();
            }
            
            ImGui.Unindent();
        }
    }
}
