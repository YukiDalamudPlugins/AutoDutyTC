using ECommons.ImGuiMethods;
using ImGuiNET;
using System.Diagnostics;

namespace AutoDuty.Windows
{
    internal static class InfoTab
    {
        static string infoUrl = "https://docs.google.com/spreadsheets/d/151RlpqRcCpiD_VbQn6Duf-u-S71EP7d0mx3j1PDNoNA";
        static string gitIssueUrl = "https://github.com/ffxivcode/AutoDuty/issues";
        static string punishDiscordUrl = "https://discord.com/channels/1001823907193552978/1236757595738476725";
        static string ffxivcodeDiscordUrl = "https://discord.com/channels/1241050921732014090/1273374407653462017";
        private static Configuration Configuration = Plugin.Configuration;

        public static void Draw()
        {
            if (MainWindow.CurrentTabName != "Info")
                MainWindow.CurrentTabName = "Info";
            ImGui.NewLine();
            ImGuiEx.TextWrapped(Loc.Get("InfoTab.SetupGuideIntro"));
            ImGui.NewLine();
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(Loc.Get("InfoTab.InformationAndSetup")).X) / 2);
            if (ImGui.Button(Loc.Get("InfoTab.InformationAndSetup")))
                Process.Start("explorer.exe", infoUrl);
            ImGui.NewLine();
            ImGuiEx.TextWrapped(Loc.Get("InfoTab.PathStatusInfo"));
            ImGui.NewLine();
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(Loc.Get("InfoTab.GitHubIssues")).X) / 2);
            if (ImGui.Button(Loc.Get("InfoTab.GitHubIssues")))
                Process.Start("explorer.exe", gitIssueUrl);
            ImGui.NewLine();
            ImGuiEx.TextCentered(Loc.Get("InfoTab.DiscordInvite"));
            ImGui.NewLine();
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(Loc.Get("InfoTab.PunishDiscord")).X) / 2);
            if (ImGui.Button(Loc.Get("InfoTab.PunishDiscord")))
                Process.Start("explorer.exe", punishDiscordUrl);
            ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(Loc.Get("InfoTab.FFXIVCodeDiscord")).X) / 2);
            if (ImGui.Button(Loc.Get("InfoTab.FFXIVCodeDiscord")))
                Process.Start("explorer.exe", ffxivcodeDiscordUrl);
        }
    }
}
