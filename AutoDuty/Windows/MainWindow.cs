using System;
using System.Collections.Generic;
using System.Numerics;
using AutoDuty.Helpers;
using AutoDuty.IPC;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.EzSharedDataManager;
using ECommons.Funding;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace AutoDuty.Windows;

public class MainWindow : Window, IDisposable
{
    internal static string CurrentTabName = "";

    private static bool _showPopup = false;
    private static bool _nestedPopup = false;
    private static string _popupText = "";
    private static string _popupTitle = "";
    private static string openTabName = "";

    public MainWindow() : base(
        $"AutoDuty v0.0.0.{Plugin.Version}###Autoduty")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(10, 10),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        TitleBarButtons.Add(new() { Icon = FontAwesomeIcon.Cog, IconOffset = new(1, 1), Click = _ => OpenTab("Config") });
        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip(Loc.Get("MainWindow.SupportKofi")), Icon = FontAwesomeIcon.Heart, IconOffset = new(1, 1), Click = _ => GenericHelpers.ShellStart("https://ko-fi.com/Herculezz") });
    }

    internal static void SetCurrentTabName(string tabName)
    {
        if (CurrentTabName != tabName)
            CurrentTabName = tabName;
    }

    internal static void OpenTab(string tabName)
    {
        openTabName = tabName;
        _ = new TickScheduler(delegate
        {
            openTabName = "";
        }, 25);
    }

    public void Dispose()
    {
    }

    internal static void Start()
    {
        ImGui.SameLine(0, 5);
    }

    internal static void LoopsConfig()
    {
        if ((Plugin.Configuration.UseSliderInputs && ImGui.SliderInt(Loc.Get("MainTab.Times"), ref Plugin.Configuration.LoopTimes, 0, 100)) || (!Plugin.Configuration.UseSliderInputs && ImGui.InputInt(Loc.Get("MainTab.Times"), ref Plugin.Configuration.LoopTimes)))
            Plugin.Configuration.Save();
    }

    internal static void StopResumePause()
    {
        using (ImRaii.Disabled(!Plugin.States.HasFlag(PluginState.Looping) && !Plugin.States.HasFlag(PluginState.Navigating) && RepairHelper.State != ActionState.Running && GotoHelper.State != ActionState.Running && GotoInnHelper.State != ActionState.Running && GotoBarracksHelper.State != ActionState.Running && GCTurninHelper.State != ActionState.Running && ExtractHelper.State != ActionState.Running && DesynthHelper.State != ActionState.Running))
        {
            if (ImGui.Button(Loc.Get("Common.Stop")))
            {
                Plugin.Stage = Stage.Stopped;
                return;
            }
            ImGui.SameLine(0, 5);
        }

        using (ImRaii.Disabled((!Plugin.States.HasFlag(PluginState.Looping) && !Plugin.States.HasFlag(PluginState.Navigating) && RepairHelper.State != ActionState.Running && GotoHelper.State != ActionState.Running && GotoInnHelper.State != ActionState.Running && GotoBarracksHelper.State != ActionState.Running && GCTurninHelper.State != ActionState.Running && ExtractHelper.State != ActionState.Running && DesynthHelper.State != ActionState.Running) || Plugin.CurrentTerritoryContent == null))
        {
            if (Plugin.Stage == Stage.Paused)
            {
                if (ImGui.Button(Loc.Get("Common.Resume")))
                {
                    Plugin.TaskManager.SetStepMode(false);
                    Plugin.Stage = Plugin.PreviousStage;
                    Plugin.States &= ~PluginState.Paused;
                }
            }
            else
            {
                if (ImGui.Button(Loc.Get("Common.Pause")))
                {
                    Plugin.Stage = Stage.Paused;
                }
            }
        }
    }

    internal static void GotoAndActions()
    {
        if(Plugin.States.HasFlag(PluginState.Other))
        {
            if(ImGui.Button(Loc.Get("Common.Stop")))
                Plugin.Stage = Stage.Stopped;
            ImGui.SameLine(0,5);
        }

        using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Looping) || Plugin.States.HasFlag(PluginState.Navigating)))
        {
            using (ImRaii.Disabled(Plugin.Configuration is { OverrideOverlayButtons: true, GotoButton: false }))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && GotoHelper.State != ActionState.Running))
                {
                    if ((GotoHelper.State == ActionState.Running && GCTurninHelper.State != ActionState.Running && RepairHelper.State != ActionState.Running) || MapHelper.State == ActionState.Running || GotoHousingHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Goto")))
                        {
                            ImGui.OpenPopup("GotoPopup");
                        }
                    }
                }
            }

            if (ImGui.BeginPopup("GotoPopup"))
            {
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.Barracks")))
                {
                    GotoBarracksHelper.Invoke();
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.Inn")))
                {
                    GotoInnHelper.Invoke();
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.GCSupply")))
                {
                    GotoHelper.Invoke(PlayerHelper.GetGrandCompanyTerritoryType(PlayerHelper.GetGrandCompany()), [GCTurninHelper.GCSupplyLocation], 0.25f, 3f);
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.FlagMarker")))
                {
                    MapHelper.MoveToMapMarker();
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.SummoningBell")))
                {
                    SummoningBellHelper.Invoke(Plugin.Configuration.PreferredSummoningBellEnum);
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.Apartment")))
                {
                    GotoHousingHelper.Invoke(Housing.Apartment);
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.PersonalHome")))
                {
                    GotoHousingHelper.Invoke(Housing.Personal_Home);
                }
                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.FCEstate")))
                {
                    GotoHousingHelper.Invoke(Housing.FC_Estate);
                }

                if (ImGui.Selectable(Loc.Get("MainWindow.Goto.TripleTriadTrader")))
                {
                    GotoHelper.Invoke(TripleTriadCardSellHelper.GoldSaucerTerritoryType, TripleTriadCardSellHelper.TripleTriadCardVendorLocation);
                }
                ImGui.EndPopup();
            }



            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(!Plugin.Configuration.AutoGCTurnin && !Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.TurninButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && GCTurninHelper.State != ActionState.Running))
                {
                    if (GCTurninHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.TurnIn")))
                        {
                            if (AutoRetainer_IPCSubscriber.IsEnabled)
                                GCTurninHelper.Invoke();
                            else
                                ShowPopup(Loc.Get("Overlay.Popup.MissingPlugin"), Loc.Get("Overlay.Tooltip.TurnInMissing"));
                        }
                        if (AutoRetainer_IPCSubscriber.IsEnabled)
                            ToolTip(Loc.Get("Overlay.Tooltip.TurnIn"));
                        else
                            ToolTip(Loc.Get("Overlay.Tooltip.TurnInMissing"));
                    }
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(!Plugin.Configuration.AutoDesynth && !Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.DesynthButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && DesynthHelper.State != ActionState.Running))
                {
                    if (DesynthHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Desynth")))
                            DesynthHelper.Invoke();
                        ToolTip(Loc.Get("Overlay.Tooltip.Desynth"));
                    }
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(!Plugin.Configuration.AutoExtract && !Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.ExtractButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && ExtractHelper.State != ActionState.Running))
                {
                    if (ExtractHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Extract")))
                        {
                            if (QuestManager.IsQuestComplete(66174))
                                ExtractHelper.Invoke();
                            else
                                ShowPopup(Loc.Get("Overlay.Popup.MissingQuestCompletion"), Loc.Get("Overlay.Tooltip.ExtractMissing"));
                        }
                        if (QuestManager.IsQuestComplete(66174))
                            ToolTip(Loc.Get("Overlay.Tooltip.Extract"));
                        else
                            ToolTip(Loc.Get("Overlay.Tooltip.ExtractMissing"));
                    }
                }
            }
            
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(!Plugin.Configuration.AutoRepair && !Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.RepairButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && RepairHelper.State != ActionState.Running))
                {
                    if (RepairHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Repair")))
                        {
                            if (InventoryHelper.CanRepair(100))
                                RepairHelper.Invoke();
                            //else
                                //ShowPopup("", "");
                        }
                        //if ()
                            ToolTip(Loc.Get("Overlay.Tooltip.Repair"));
                        //else
                            //ToolTip("");
                    }
                }
            }
            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(!Plugin.Configuration.AutoEquipRecommendedGear && !Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.EquipButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && AutoEquipHelper.State != ActionState.Running))
                {
                    if (AutoEquipHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Equip")))
                        {
                            AutoEquipHelper.Invoke();
                            //else
                            //ShowPopup("", "");
                        }

                        //if ()
                        ToolTip(Loc.Get("Overlay.Tooltip.Equip"));
                        //else
                        //ToolTip("");
                    }
                }
            }

            ImGui.SameLine(0, 5);
            using (ImRaii.Disabled(Plugin.Configuration is { AutoOpenCoffers: false, OverrideOverlayButtons: false } || !Plugin.Configuration.CofferButton))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other) && CofferHelper.State != ActionState.Running))
                {
                    if (CofferHelper.State == ActionState.Running)
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.Coffers")))
                            CofferHelper.Invoke();
                        ToolTip(Loc.Get("Overlay.Tooltip.Coffers"));
                    }
                }
            }
            ImGui.SameLine(0, 5);

            using (ImRaii.Disabled(!Plugin.Configuration.TripleTriadEnabled && (!Plugin.Configuration.OverrideOverlayButtons || !Plugin.Configuration.TTButton)))
            {
                using (ImRaii.Disabled(Plugin.States.HasFlag(PluginState.Other)))
                {
                    if ((GotoHelper.State == ActionState.Running && TripleTriadCardUseHelper.State != ActionState.Running && TripleTriadCardSellHelper.State != ActionState.Running))
                    {
                        if (ImGui.Button(Loc.Get("Common.Stop")))
                            Plugin.Stage = Stage.Stopped;
                    }
                    else
                    {
                        if (ImGui.Button(Loc.Get("Overlay.Button.TripleTriad")))
                            ImGui.OpenPopup("TTPopup");
                    }
                }
            }

            if (ImGui.BeginPopup("TTPopup"))
            {
                if (ImGui.Selectable(Loc.Get("MainWindow.TT.RegisterCards")))
                    TripleTriadCardUseHelper.Invoke();
                if (ImGui.Selectable(Loc.Get("MainWindow.TT.SellCards")))
                    TripleTriadCardSellHelper.Invoke();
                ImGui.EndPopup();
            }
        }
    }

    internal static void ToolTip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGuiEx.Text(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
    }

    internal static void ShowPopup(string popupTitle, string popupText, bool nested = false)
    {
        _popupTitle = popupTitle;
        _popupText = popupText;
        _showPopup = true;
        _nestedPopup = nested;
    }

    internal static void DrawPopup(bool nested = false)
    {
        if (!_showPopup || (_nestedPopup && !nested) || (!_nestedPopup && nested)) return;

        if (!ImGui.IsPopupOpen($"{_popupTitle}###Popup"))
            ImGui.OpenPopup($"{_popupTitle}###Popup");

        Vector2 textSize = ImGui.CalcTextSize(_popupText);
        ImGui.SetNextWindowSize(new(textSize.X + 25, textSize.Y + 100));
        if (ImGui.BeginPopupModal($"{_popupTitle}###Popup", ref _showPopup, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove))
        {
            ImGuiEx.TextCentered(_popupText);
            ImGui.Spacing();
            if (ImGuiHelper.CenteredButton(Loc.Get("MainWindow.OK"), .5f, 15))
            {
                _showPopup = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void KofiLink()
    {
        OpenTab(CurrentTabName);
        if (EzThrottler.Throttle("KofiLink", 15000))
        {
            _ = new TickScheduler(delegate
            {
                GenericHelpers.ShellStart("https://ko-fi.com/Herculezz");
            }, 500);
        }
    }

    //ECommons
    static uint ColorNormal
    {
        get
        {
            var vector1 = ImGuiEx.Vector4FromRGB(0x022594);
            var vector2 = ImGuiEx.Vector4FromRGB(0x940238);

            var gen = GradientColor.Get(vector1, vector2).ToUint();
            var data = EzSharedData.GetOrCreate<uint[]>("ECommonsPatreonBannerRandomColor", [gen]);
            if (!GradientColor.IsColorInRange(data[0].ToVector4(), vector1, vector2))
            {
                data[0] = gen;
            }
            return data[0];
        }
    }
    public static void EzTabBar(string id, string? KoFiTransparent, string openTabName, ImGuiTabBarFlags flags, params (string name, Action function, Vector4? color, bool child)[] tabs)
    {
        ImGui.BeginTabBar(id, flags);
        foreach (var x in tabs)
        {
            if (x.name == null) continue;
            if (x.color != null)
            {
                ImGui.PushStyleColor(ImGuiCol.Tab, x.color.Value);
            }
            if (ImGuiEx.BeginTabItem(Loc.Get($"MainWindow.Tabs.{x.name}") + "###" + x.name, openTabName == x.name ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
            {
                if (x.color != null) 
                    ImGui.PopStyleColor();
                if (x.child) 
                    ImGui.BeginChild(x.name + "child");
                x.function();
                if (x.child) 
                    ImGui.EndChild();
                ImGui.EndTabItem();
            }
            else
            {
                if (x.color != null)
                {
                    ImGui.PopStyleColor();
                }
            }
        }
        if (KoFiTransparent != null) PatreonBanner.RightTransparentTab();
        ImGui.EndTabBar();
    }

    private static readonly List<(string, Action, Vector4?, bool)> tabList =
        [("Main", MainTab.Draw, null, false), ("Build", BuildTab.Draw, null, false), ("Paths", PathsTab.Draw, null, false), ("Config", ConfigTab.Draw, null, false), ("Info", InfoTab.Draw, null, false), ("Logs", LogTab.Draw, null, false),("Support", KofiLink, ImGui.ColorConvertU32ToFloat4(ColorNormal), false)
        ];

    public override void Draw()
    {
        DrawPopup();

        if(DalamudInfoHelper.IsOnStaging())
        {
            ImGui.TextColored(GradientColor.Get(ImGuiHelper.ExperimentalColor, ImGuiHelper.ExperimentalColor2, 500), Loc.Get("MainWindow.Staging.NotSupported"));
            ImGui.Text(Loc.Get("MainWindow.Staging.PickRelease"));

            if (!ImGui.CollapsingHeader(Loc.Get("MainWindow.Staging.UseAnyway") + "##stagingHeader"))
                return;
        }

        EzTabBar("MainTab", null, openTabName, ImGuiTabBarFlags.None, tabList.ToArray());
    }
}
