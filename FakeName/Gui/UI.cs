using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Utility;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using ImGuiNET;

namespace FakeName.Gui;

public class UI
{
    public static void Draw()
    {
        if (EzThrottler.Throttle("PeriodicConfigSave", 30 * 1000))
        {
            Svc.PluginInterface.SavePluginConfig(P.Config);
            EzConfig.Save();
        }

        ImGui.Checkbox("Enabled", ref C.Enabled);
        ImGuiEx.EzTabBar("##main",[
            ("Characters", TabCharacter.Draw, null, true),
            ("Debug", TabDebug.Draw, null, true),
        ]);
        TabCharacter.Draw();
    }

    private static float ButtonOffset;
}
