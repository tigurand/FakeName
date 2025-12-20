using Dalamud.Bindings.ImGui;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;

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

    var enabled = C.Enabled;
    if (ImGui.Checkbox("Enabled", ref enabled))
    {
      C.Enabled = enabled;
      P.NamePlate?.ForceRedraw();
      P.PartyList?.ForceRedraw();
    }

    ImGuiEx.EzTabBar("##main", [
      ("Characters", TabCharacter.Draw, null, true),
      ("Debug", TabDebug.Draw, null, true),
    ]);
  }

  //private static float ButtonOffset;
}
