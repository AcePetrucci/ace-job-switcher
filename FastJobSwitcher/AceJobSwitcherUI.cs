using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace AceJobSwitcher;

public class AceJobSwitcherUI : Window, IDisposable
{
    private readonly ConfigurationMKII configuration;

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
            MaximumSize = new Vector2(268, 1000)
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
            ImGui.TextWrapped("Command suffixes add variants like /blmucob, /blmtea for ultimates and /blmeu, /blmbo for field operations.");
        }
    }

    // public override void Draw()
    // {
    //     ImGui.TextWrapped("Register commands for each class/job:");
    //     ImGui.Indent();
    //     {
    //         var lowercaseEnabled = configuration.RegisterLowercaseCommands;
    //         if (ImGui.Checkbox("Lowercase##LowercaseCommand", ref lowercaseEnabled))
    //         {
    //             configuration.RegisterLowercaseCommands = lowercaseEnabled;
    //             configuration.Save();
    //         }

    //         var uppercaseEnabled = configuration.RegisterUppercaseCommands;
    //         if (ImGui.Checkbox("Uppercase##UppercaseCommand", ref uppercaseEnabled))
    //         {
    //             configuration.RegisterUppercaseCommands = uppercaseEnabled;
    //             configuration.Save();
    //         }
    //     }
    //     ImGui.Unindent();

    //     ImGui.NewLine();
    //     ImGui.TextWrapped("Optional Prefix/Suffix for each command:");
    //     ImGui.Indent();
    //     {
    //         ImGui.BeginTable("##table", 2);

    //         ImGui.TableNextRow();
    //         ImGui.TableSetColumnIndex(0);
    //         ImGui.Text("Prefix:");
    //         ImGui.TableSetColumnIndex(1);
    //         ImGui.SetNextItemWidth(180);
    //         var prefix = configuration.Prefix;
    //         if (ImGui.InputText("##Prefix", ref prefix, 32))
    //         {
    //             configuration.Prefix = prefix;
    //             configuration.Save();
    //         }

    //         ImGui.TableNextRow();
    //         ImGui.TableSetColumnIndex(0);
    //         ImGui.Text("Suffix:");
    //         ImGui.TableSetColumnIndex(1);
    //         var suffix = configuration.Suffix;
    //         ImGui.SetNextItemWidth(180);
    //         if (ImGui.InputText("##Suffix", ref suffix, 32))
    //         {
    //             configuration.Suffix = suffix;
    //             configuration.Save();
    //         }

    //         ImGui.EndTable();
    //         ImGui.TextWrapped("(The casing of the Prefix/Suffix is determined by the casing of the job command)");
    //     }
    //     ImGui.Unindent();
    // }
}
