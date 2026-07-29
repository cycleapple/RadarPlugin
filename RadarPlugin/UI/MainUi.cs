using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using RadarPlugin.Constants;
using RadarPlugin.Enums;
using RadarPlugin.RadarLogic;

namespace RadarPlugin.UI;

public class MainUi : IDisposable
{
    private Configuration.Configuration configInterface;
    private readonly IDalamudPluginInterface dalamudPluginInterface;
    private readonly LocalMobsUi localMobsUi;
    private bool mainWindowVisible;
    private readonly IClientState clientState;
    private readonly TypeConfigurator typeConfigurator;
    private readonly CustomizedEntitiesUI customizedEntitiesUi;
    private readonly IPluginLog pluginLog;
    private readonly RadarModules radarModules;

    public MainUi(
        IDalamudPluginInterface dalamudPluginInterface,
        Configuration.Configuration configInterface,
        LocalMobsUi localMobsUi,
        IClientState clientState,
        TypeConfigurator typeConfigurator,
        CustomizedEntitiesUI customizedEntitiesUi,
        IPluginLog pluginLog,
        RadarModules radarModules
    )
    {
        this.clientState = clientState;
        this.localMobsUi = localMobsUi;
        this.configInterface = configInterface;
        this.dalamudPluginInterface = dalamudPluginInterface;
        this.typeConfigurator = typeConfigurator;
        this.dalamudPluginInterface.UiBuilder.Draw += Draw;
        this.dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenUi;
        this.customizedEntitiesUi = customizedEntitiesUi;
        this.pluginLog = pluginLog;
        this.radarModules = radarModules;
    }

    public void Dispose()
    {
        this.dalamudPluginInterface.UiBuilder.Draw -= Draw;
        this.dalamudPluginInterface.UiBuilder.OpenConfigUi -= OpenUi;
    }

    public void OpenUi()
    {
        mainWindowVisible = true;
    }

    private void Draw()
    {
        DrawMainWindow();
    }

    private void DrawMainWindow()
    {
        if (!mainWindowVisible)
        {
            return;
        }

        var size = new Vector2(480, 440);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(size, new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("Radar Plugin", ref mainWindowVisible))
        {
            UiHelpers.DrawTabs(
                "radar-settings-tabs",
                ("一般", ConfigConstants.White, DrawGeneralSettings),
                ("目標設定", ConfigConstants.Red, DrawVisibilitySettings),
                ("2D 雷達", Color.White, Draw2DRadarSettings),
                ("附加功能", ConfigConstants.White, ShowMiscSettings),
                ("工具", ConfigConstants.White, DrawUtilityTab),
                ("設定檔", ConfigConstants.White, DrawConfigTab)
            );
        }

        ImGui.End();
    }

    private void Draw2DRadarSettings()
    {
        UiHelpers.Draw2DRadarSettings(ref configInterface.cfg.Radar2DConfiguration);
    }

    private void DrawConfigTab()
    {
        var shouldSave = false;
        ImGui.Text("目前設定檔名稱：");
        shouldSave |= ImGui.InputText("", ref configInterface.cfg.ConfigName, 50);

        if (ImGui.Button("儲存目前設定檔"))
        {
            this.configInterface.SaveCurrentConfig();
        }

        ImGui.Text("已儲存的設定檔：");
        var selectedConfig = configInterface.selectedConfig;
        if (
            ImGui.Combo(
                "##selectedconfigcombobox",
                ref selectedConfig,
                configInterface.configs,
                configInterface.configs.Length
            )
        )
        {
            if (selectedConfig < configInterface.configs.Length)
            {
                configInterface.selectedConfig = selectedConfig;
            }
        }

        if (ImGui.Button("載入所選設定檔"))
        {
            this.configInterface.LoadConfig(configInterface.configs[selectedConfig]);
        }

        UiHelpers.HoverTooltip("載入時會先儲存目前設定檔！");
        ImGui.SameLine();
        if (ImGui.Button("刪除所選設定檔"))
        {
            ImGui.OpenPopup("DeleteConfigPopup");
        }

        if (ImGui.Button("新增空白設定檔"))
        {
            this.configInterface.SaveNewDefaultConfig();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.Border, ImGui.GetColorU32(ImGuiCol.TabActive));
        if (ImGui.BeginPopup("DeleteConfigPopup"))
        {
            ImGui.TextColored(
                ImGui.ColorConvertU32ToFloat4(ConfigConstants.White),
                $"確定要刪除設定檔「{configInterface.configs[selectedConfig]}」嗎？"
            );
            if (ImGui.Button("是"))
            {
                this.configInterface.DeleteConfig(configInterface.configs[selectedConfig]);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ConfigConstants.Red);
            if (ImGui.Button("否"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.PopStyleColor();
            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        if (shouldSave)
            configInterface.Save();
    }

    private void DrawUtilityTab()
    {
        var shouldSave = false;

        ImGui.Separator();

        if (ImGui.Button("載入目前區域物件清單"))
        {
            pluginLog.Debug("Pulling Area Objects");
            this.localMobsUi.DrawLocalMobsUi();
        }

        ImGui.Separator();

        ImGui.Text($"目前地圖 ID：{clientState.TerritoryType}");
        ImGui.Text($"區域類型：{radarModules.zoneTypeModule.GetLocationType()}");
        if (ImGui.Button("顯示目前自訂項目"))
        {
            customizedEntitiesUi.ShowCustomizedEntitiesUI();
        }

        if (configInterface.cfg.DebugMode)
        {
            // Todo: Debug swap text bools
        }

        if (shouldSave)
            configInterface.Save();
    }

    private void DrawGeneralSettings()
    {
        var shouldSave = false;
        UiHelpers.TextColored("由 KangasZ 製作，供 FFXIV 使用。", ConfigConstants.Yellow);
        shouldSave |= UiHelpers.DrawCheckbox("啟用插件", ref configInterface.cfg.Enabled);

        shouldSave |= UiHelpers.DrawCheckbox(
            "啟用 3D 雷達",
            ref configInterface.cfg.Radar3DEnabled,
            "啟用 3D 雷達。"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            "啟用 2D 雷達",
            ref configInterface.cfg.Radar2DConfiguration.Enabled,
            "啟用 2D 雷達。"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            "啟用野外顯示",
            ref configInterface.cfg.ShowOverworldObjects,
            "啟用後會顯示野外區域中的物件，停用時不會顯示。"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            "支援優雷卡／蜃景幻界／深層迷宮",
            ref configInterface.cfg.ShowBaDdObjects,
            "著重支援優雷卡與深層迷宮，會顯示傳送門、寶箱及陷阱等物件。"
        );

        shouldSave |= UiHelpers.DrawNumberInputBox(
            "你的帳號 ID",
            ref configInterface.cfg.YourAccountId,
            "This will use your account id to deobfuscate others account ids.\n"
                + "I will update this to support alternate accounts sometime probably.\n"
                + "You MUST know your account id\n"
                + "If you know another persons account id you can derrive your own (make an issue if this is something you want)\n"
                + "If you saved your old account id via a custom save, you can use that"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            "使用背景繪製清單",
            ref configInterface.cfg.UseBackgroundDrawList,
            "使用 ImGui 背景繪製清單顯示 3D 雷達。\n"
                + "雷達會位於其他 Dalamud 插件下層，這是原本的顯示方式；正常使用時幾乎沒有差異。"
        );

        shouldSave |= UiHelpers.DrawDotSizeSlider(
            ref configInterface.cfg.DotSize,
            "default-dot-size",
            "3D 點位大小"
        );
        shouldSave |= UiHelpers.DrawDotSizeSlider(
            ref configInterface.cfg.DotSize2D,
            "default-dot-size-3d",
            "2D 點位大小"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            "除錯文字",
            ref configInterface.cfg.DebugText,
            "以「名稱、DataId、目標類型」除錯字串取代名稱。\n/radar showdebug"
        );

        if (ImGui.CollapsingHeader("篩選規則"))
        {
            shouldSave |= UiHelpers.DrawCheckbox(
                "最大距離",
                ref configInterface.cfg.UseMaxDistance,
                "ESP 顯示的最大距離。"
            );
            if (configInterface.cfg.UseMaxDistance)
            {
                ImGui.SameLine();
                shouldSave |= UiHelpers.DrawFloatWithResetSlider(
                    ref configInterface.cfg.MaxDistance,
                    "",
                    "default-max-distance-size",
                    1f,
                    2000f,
                    ConfigConstants.DefaultMaxEspDistance,
                    "%.0fm"
                );
            }

            shouldSave |= UiHelpers.DrawCheckbox(
                "只顯示可見目標",
                ref configInterface.cfg.ShowOnlyVisible,
                "只顯示可見目標，通常建議保持啟用。\n目前可能無法排除所有不可見目標，可使用區域物件清單處理。"
            );

            if (configInterface.cfg.ShowOnlyVisible)
            {
                ImGui.SameLine();
                shouldSave |= UiHelpers.DrawCheckbox(
                    "顯示不可見玩家角色",
                    ref configInterface.cfg.OverrideShowInvisiblePlayerCharacters,
                    "顯示不可見的玩家角色，例如正在載入的玩家；不保證能套用於 GM。"
                );
            }

            UiHelpers.DrawCheckbox(
                "顯示無名稱目標",
                ref configInterface.cfg.ShowNameless,
                "顯示沒有名稱的目標，通常建議保持停用。"
            );
            UiHelpers.DrawCheckbox(
                "顯示所有目標",
                ref configInterface.cfg.DebugMode,
                "忽略其他條件並顯示所有目標。\n/radar showall"
            );
            UiHelpers.DrawCheckbox(
                "顯示位階文字",
                ref configInterface.cfg.RankText,
                "顯示戰鬥 NPC 的位階文字，通常與狩獵及首領目標相關；此為除錯功能。"
            );
        }

        ImGui.Separator();
        UiHelpers.TextColored("感謝你的支持！", ConfigConstants.Red);
        ImGui.Separator();
        UiHelpers.TextColored("問題與意見回饋：", ConfigConstants.Red);
        ImGui.SameLine();
        UiHelpers.TextURL(
            "GitHub",
            "https://github.com/KangasZ/RadarPlugin",
            ImGui.GetColorU32(ImGuiCol.Text)
        );
        ImGui.Indent();
        UiHelpers.TextColored(
            "1. 使用各分頁自訂顯示方式並處理不可見目標。\n"
                + "2. 歡迎提出錯誤或功能需求。\n",
            ConfigConstants.Yellow
        );
        ImGui.Unindent();
        ImGui.Spacing();
        ImGui.TextWrapped(
            "注意 1：客戶端尚未載入的目標無法顯示，例如深層迷宮陷阱必須被揭露後才可見。"
        );
        ImGui.Spacing();
        ImGui.TextWrapped(
            "注意 2：可能會顯示不可見目標，請使用「工具」分頁排除。"
        );
        if (shouldSave)
            configInterface.Save();
    }

    private bool DrawFontOptions()
    {
        var shouldSave = false;

        shouldSave |= ImGui.Checkbox(
            "使用自訂字型##custom-font-selector-default",
            ref configInterface.cfg.FontSettings.UseCustomFont
        );
        UiHelpers.LabeledHelpMarker(
            "",
            "使用自訂字型大小及字型取代預設 ImGui 字型。\n首次啟用時可能造成短暫卡頓。"
        );

        shouldSave |= ImGui.Checkbox(
            "使用 Axis 字型##axis-font-selector-default",
            ref configInterface.cfg.FontSettings.UseAxisFont
        );
        UiHelpers.LabeledHelpMarker(
            "",
            "使用 Axis 字型取代 Dalamud 預設字型；遊戲內大多數文字使用此字型。"
        );

        shouldSave |= UiHelpers.DrawFloatWithResetSlider(
            ref configInterface.cfg.FontSettings.FontSize,
            "字型大小",
            "font-scale-default-window",
            7f,
            36f,
            ImGui.GetFontSize(),
            "%.0fpx"
        );

        if (ImGui.Button("12px"))
        {
            configInterface.cfg.FontSettings.FontSize = 12f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("16px"))
        {
            configInterface.cfg.FontSettings.FontSize = 16f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("17px"))
        {
            configInterface.cfg.FontSettings.FontSize = 17f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("18px"))
        {
            configInterface.cfg.FontSettings.FontSize = 18f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("20px"))
        {
            configInterface.cfg.FontSettings.FontSize = 20f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("22px"))
        {
            configInterface.cfg.FontSettings.FontSize = 22f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("24px"))
        {
            configInterface.cfg.FontSettings.FontSize = 24f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("36px"))
        {
            configInterface.cfg.FontSettings.FontSize = 36f;
            shouldSave = true;
        }

        return shouldSave;
    }

    private void DrawVisibilitySettings()
    {
        UiHelpers.DrawTabs(
            "radar-visibility-tabs",
            ("玩家", ConfigConstants.Silver, DrawPlayerGeneralSettings),
            ("敵人", ConfigConstants.Green, DrawMobsVisibilitySettings),
            ("物件", ConfigConstants.LightBlue, DrawEntitiesVisibilitySettings),
            ("特殊探索／深層迷宮", ConfigConstants.Yellow, DrawDeepDungeonOverviewSettings)
        );
    }

    private void DrawPlayerGeneralSettings()
    {
        var shouldSave = false;
        UiHelpers.DrawSeperator("玩家", ConfigConstants.Red);
        DrawSettingsOverview(configInterface.cfg.PlayerOption, "玩家", mobType: MobType.Player);

        // Custom YOUR PLAYER that I don't want to deal with yet.\
        ImGui.Separator();
        ImGui.PushStyleColor(ImGuiCol.Text, ConfigConstants.Red);
        ImGui.Text("個別分類");
        ImGui.PopStyleColor();
        UiHelpers.LabeledHelpMarker(
            "",
            "These options will dissociate the given category with the overriding player configuration.\n"
                + "Any player not in one of these categories will default to 'general' player option."
        );
        ImGui.Separator();
        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedYourPlayer,
            "自己",
            MobType.Player
        );

        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedParty,
            "小隊",
            MobType.Player
        );

        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedFriends,
            "好友",
            MobType.Player
        );

        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedAlliance,
            "團隊",
            MobType.Player
        );

        if (shouldSave)
            configInterface.Save();
    }

    private void DrawDeepDungeonOverviewSettings()
    {
        UiHelpers.DrawSeperator("敵人選項", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.SpecialUndeadOption,
            "特殊不死系敵人",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption,
            "其他敵人",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.AuspiceOption,
            "友方目標",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.EasyMobOption,
            "簡單敵人",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.MimicOption,
            "寶箱怪",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.PatrolOption,
            "巡邏敵人",
            mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        
        UiHelpers.DrawSeperator("戰利品選項", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.GoldChestOption,
            "金寶箱",
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.SilverChestOption,
            "銀寶箱",
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.BronzeChestOption,
            "銅寶箱",
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.AccursedHoardOption,
            "埋藏的寶藏",
            displayOrigination: DisplayOrigination.DeepDungeon
        );

        UiHelpers.DrawSeperator("深層迷宮專用選項", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.TrapOption,
            "陷阱",
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.ReturnOption,
            "重生石塔",
            displayOrigination: DisplayOrigination.DeepDungeon
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.PassageOption,
            "轉移石塔",
            displayOrigination: DisplayOrigination.DeepDungeon,
            description: "也包含蜃景幻界中的部分混合物件。"
        );
        DrawSettingsOverview(
            configInterface.cfg.DeepDungeonOptions.ActivatableOption,
            "可啟動物件",
            displayOrigination: DisplayOrigination.DeepDungeon,
            description: "包含燭台，以及蜃景幻界、南方博茲雅戰線與優雷卡中的部分可啟動地點。"
        );
    }

    private void DrawSettingsOverview(
        Configuration.Configuration.ESPOption espOption,
        string tag,
        string? description = null,
        MobType mobType = MobType.Object,
        DisplayOrigination displayOrigination = DisplayOrigination.OpenWorld
    )
    {
        bool shouldSave = false;
        shouldSave |= UiHelpers.DrawDisplayTypesEnumListBox(
            "",
            $"visibilitygeneralsettings-enum-{tag}",
            mobType,
            ref espOption.DisplayTypeFlags
        );
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"##visiblitygeneralsettings-color-{tag}",
            ref espOption.ColorU
        );

        ImGui.SameLine();

        shouldSave |= ImGui.Checkbox($"{tag}##enabled-{tag}", ref espOption.Enabled);

        if (!description.IsNullOrWhitespace())
        {
            UiHelpers.LabeledHelpMarker("", description);
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 100);
        if (ImGui.Button($"更多選項##button-for-type-configurator{tag}"))
        {
            typeConfigurator.OpenUiWithType(ref espOption, tag, mobType, displayOrigination);
        }

        if (shouldSave)
            configInterface.Save();
    }

    private void ShowMiscSettings()
    {
        var shouldSave = false;
        var id = "##miscsettings";
        ImGui.BeginChild($"{id}-child", new Vector2(0, 0));

        if (ImGui.CollapsingHeader($"碰撞箱選項{id}"))
        {
            shouldSave |= ImGui.Checkbox(
                $"顯示碰撞箱{id}-hitbox",
                ref configInterface.cfg.HitboxOptions.HitboxEnabled
            );

            shouldSave |= ImGui.DragFloat(
                $"線條粗細{id}",
                ref configInterface.cfg.HitboxOptions.Thickness,
                0.1f,
                0.1f,
                14f
            );

            shouldSave |= ImGui.Checkbox(
                $"覆寫目標顏色{id}-hitbox",
                ref configInterface.cfg.HitboxOptions.OverrideMobColor
            );

            if (configInterface.cfg.HitboxOptions.OverrideMobColor)
            {
                var hitboxColor = ImGui.ColorConvertU32ToFloat4(
                    configInterface.cfg.HitboxOptions.HitboxColor
                );
                if (
                    ImGui.ColorEdit4(
                        $"顏色{id}-hitbox-color",
                        ref hitboxColor,
                        ImGuiColorEditFlags.NoInputs
                    )
                )
                {
                    configInterface.cfg.HitboxOptions.HitboxColor = ImGui.ColorConvertFloat4ToU32(
                        hitboxColor
                    );
                    configInterface.Save();
                }
            }

            shouldSave |= ImGui.Checkbox(
                $"填滿碰撞箱內部{id}-hitbox",
                ref configInterface.cfg.HitboxOptions.DrawInsideCircle
            );

            if (configInterface.cfg.HitboxOptions.DrawInsideCircle)
            {
                shouldSave |= ImGui.Checkbox(
                    "使用不同的內部顏色",
                    ref configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor
                );

                if (!configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor)
                {
                    var circleOpacity =
                        (float)(configInterface.cfg.HitboxOptions.InsideCircleOpacity >> 24)
                        / byte.MaxValue;
                    if (
                        ImGui.DragFloat(
                            $"內部圓形透明度{id}",
                            ref circleOpacity,
                            0.005f,
                            0,
                            1
                        )
                    )
                    {
                        configInterface.cfg.HitboxOptions.InsideCircleOpacity =
                            ((uint)(circleOpacity * 255) << 24) | 0x00FFFFFF;
                        configInterface.Save();
                    }
                }

                if (configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor)
                {
                    shouldSave |= UiHelpers.Vector4ColorSelector(
                        $"內部圓形顏色{id}-hitbox",
                        ref configInterface.cfg.HitboxOptions.InsideCircleColor
                    );
                }
            }
        }

        if (ImGui.CollapsingHeader("敵意偵測半徑選項"))
        {
            DrawAggroCircleSettings();
        }

        if (ImGui.CollapsingHeader("畫面外目標設定"))
        {
            shouldSave |= ImGui.Checkbox(
                "顯示畫面外目標",
                ref configInterface.cfg.ShowOffScreen
            );
            UiHelpers.HoverTooltip("以箭頭顯示位於畫面外的敵人。");

            shouldSave |= ImGui.DragFloat(
                $"與畫面邊緣的距離{id}",
                ref configInterface.cfg.OffScreenObjectsOptions.DistanceFromEdge,
                0.2f,
                2f,
                80f
            );

            shouldSave |= ImGui.DragFloat(
                $"大小{id}",
                ref configInterface.cfg.OffScreenObjectsOptions.Size,
                0.1f,
                2f,
                20f
            );

            shouldSave |= ImGui.DragFloat(
                $"線條粗細{id}",
                ref configInterface.cfg.OffScreenObjectsOptions.Thickness,
                0.1f,
                0.4f,
                20f
            );
        }

        if (ImGui.CollapsingHeader("字型設定"))
        {
            shouldSave |= DrawFontOptions();
        }

        if (ImGui.CollapsingHeader("依等級顯示"))
        {
            var levelRenderingSettings = configInterface.cfg.LevelRendering;
            shouldSave |= UiHelpers.DrawCheckbox(
                "啟用",
                ref levelRenderingSettings.LevelRenderingEnabled,
                "依相對等級篩選顯示目標。"
            );
            if (levelRenderingSettings.LevelRenderingEnabled)
            {
                shouldSave |= UiHelpers.DrawIntWithResetSlider(
                    ref levelRenderingSettings.RelativeLevelsBelow,
                    "顯示的相對等級",
                    "level-based-rel-level",
                    1,
                    89,
                    20
                );
                DrawSettingsOverview(
                    levelRenderingSettings.LevelRenderEspOption,
                    "符合等級的敵人",
                    "",
                    MobType.Character
                );
            }
        }

        if (ImGui.CollapsingHeader("實驗性目標計時器"))
        {
            shouldSave |= UiHelpers.DrawCheckbox(
                "目標計時器",
                ref configInterface.cfg.EXPERIMENTALEnableMobTimerTracking,
                "在深層迷宮與優雷卡追蹤目標計時。"
            );
        }

        ImGui.EndChild();
        if (shouldSave)
            configInterface.Save();
    }

    private void DrawAggroCircleSettings()
    {
        var shouldSave = false;

        var tag = "aggroradiusoptions";

        shouldSave |= UiHelpers.DrawCheckbox(
            $"敵意偵測圓##{tag}-settings",
            ref configInterface.cfg.AggroRadiusOptions.ShowAggroCircle,
            "繪製敵意偵測圓。"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            $"限制敵意偵測半徑的最大顯示距離##{tag}-max-dist",
            ref configInterface.cfg.AggroRadiusOptions.MaxDistanceCapBool,
            "設定敵意偵測圓的最大顯示距離。"
        );
        shouldSave |= UiHelpers.DrawFloatWithResetSlider(
            ref configInterface.cfg.AggroRadiusOptions.MaxDistance,
            "",
            $"##{tag}-max-dist-slider",
            1f,
            2000f,
            ConfigConstants.DefaultMaxAggroRadiusDistance,
            "%.0fm"
        );

        shouldSave |= UiHelpers.DrawCheckbox(
            $"戰鬥中顯示敵意偵測圓##{tag}-settings",
            ref configInterface.cfg.AggroRadiusOptions.ShowAggroCircleInCombat,
            "啟用時一律顯示敵意偵測圓；停用時只在敵人尚未進入戰鬥時顯示。"
        );
        UiHelpers.DrawSeperator("視覺偵測設定：", ConfigConstants.White);
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"正面##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.FrontColor
        );
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"背面##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.RearColor
        );

        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"左側##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.LeftSideColor
        );
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"右側##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.RightSideColor
        );
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"正面扇形##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.FrontConeColor
        );
        UiHelpers.DrawSeperator("聲音偵測設定：", ConfigConstants.White);
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"聲音##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.SoundAggroColor
        );

        UiHelpers.DrawSeperator("接近偵測設定：", ConfigConstants.White);
        shouldSave |= UiHelpers.Vector4ColorSelector(
            $"接近##{tag}",
            ref configInterface.cfg.AggroRadiusOptions.ProximityAggroColor
        );

        if (shouldSave)
            configInterface.Save();
    }

    private void DrawMobsVisibilitySettings()
    {
        bool shouldSave = false;
        ImGui.BeginChild($"##visiblitygeneralsettings-radar-tabs-child", new Vector2(0, 0));

        UiHelpers.DrawSeperator("非敵對 NPC", ConfigConstants.Red);
        DrawSettingsOverview(configInterface.cfg.CompanionOption, "搭檔");
        DrawSettingsOverview(configInterface.cfg.EventNpcOption, "事件 NPC");
        DrawSettingsOverview(configInterface.cfg.RetainerOption, "僱員");

        UiHelpers.DrawSeperator("敵對 NPC", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.NpcOption,
            "敵人",
            description: "顯示大多數可進行戰鬥的敵人。",
            mobType: MobType.Character
        );
        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedRankOne,
            "位階 1",
            MobType.Character,
            "位階 1 通常是狩獵或稀有敵人。"
        );
        shouldSave |= DrawBoolSeparatedSettingsOverview(
            ref configInterface.cfg.SeparatedRankTwoAndSix,
            "位階 2 與 6",
            MobType.Character,
            "位階 2 與 6 通常是首領。"
        );
        ImGui.EndChild();
        if (shouldSave)
            configInterface.Save();
    }

    private void DrawEntitiesVisibilitySettings()
    {
        bool shouldSave = false;
        ImGui.BeginChild($"##visiblitygeneralsettings-radar-tabs-child", new Vector2(0, 0));

        UiHelpers.DrawSeperator("物件", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.TreasureOption,
            "寶箱",
            "顯示大多數戰利品，分類方式依 Dalamud 的物件關聯。"
        );
        DrawSettingsOverview(configInterface.cfg.EventObjOption, "事件物件");
        DrawSettingsOverview(configInterface.cfg.AreaOption, "區域物件");
        DrawSettingsOverview(configInterface.cfg.AetheryteOption, "傳送水晶");
        DrawSettingsOverview(
            configInterface.cfg.CutsceneOption,
            "劇情物件",
            "顯示劇情使用的物件。"
        );

        UiHelpers.DrawSeperator("其他", ConfigConstants.Red);
        DrawSettingsOverview(
            configInterface.cfg.CardStandOption,
            "卡片架",
            "顯示卡片架，也包含大多數無人島相關物件。"
        );
        DrawSettingsOverview(
            configInterface.cfg.GatheringPointOption,
            "採集點",
            "顯示採集點。"
        );
        DrawSettingsOverview(
            configInterface.cfg.MountOption,
            "坐騎",
            "顯示坐騎，目標較多時可能顯得雜亂。"
        );
        DrawSettingsOverview(
            configInterface.cfg.OrnamentOption,
            "時尚配飾",
            "顯示翅膀等時尚配飾。"
        );
        ImGui.EndChild();
        if (shouldSave)
            configInterface.Save();
    }

    private bool DrawBoolSeparatedSettingsOverview(
        ref Configuration.Configuration.SeparatedEspOption separatedEspOption,
        string tag,
        MobType mobType,
        string? infoDescription = null
    )
    {
        var shouldSave = false;
        shouldSave |= ImGui.Checkbox(
            $"個別設定：{tag}##separated-settings",
            ref separatedEspOption.Enabled
        );
        if (infoDescription != null)
        {
            UiHelpers.LabeledHelpMarker($"", infoDescription);
        }

        if (separatedEspOption.Enabled)
        {
            DrawSettingsOverview(
                separatedEspOption.EspOption,
                tag,
                mobType: mobType,
                description: infoDescription
            );
        }

        return shouldSave;
    }
}
