using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FakeName.Data;
using FakeName.OtterGuiHandlers;
using OtterGui.Raii;
using System.Numerics;

namespace FakeName.Gui;

internal class TabCharacter
{
  static CharacterConfig Selected => P.OtterGuiHandler.FakeNameFileSystem.Selector.Selected;

  public static void Draw()
  {
    using (var child = ImRaii.Child("##Selector", new Vector2(200, 0), true))
    {
        if (child)
            P.OtterGuiHandler.FakeNameFileSystem.Selector.Draw();
    }
    ImGui.SameLine();
    using var group = ImRaii.Group();
    DrawHeader();
    DrawSelected();
  }

  private static void DrawHeader()
  {
    HeaderDrawer.Draw(
      P.OtterGuiHandler.FakeNameFileSystem.FindLeaf(Selected, out var l) ? $"{l.Value.IncognitoName()}({l.Value.WorldName()})" : "", 0,
      ImGui.GetColorU32(ImGuiCol.FrameBg), 0,
      HeaderDrawer.Button.IncognitoButton(C.IncognitoMode, v => C.IncognitoMode = v)
    );
  }

  public static void DrawSelected()
  {
    using var child = ImRaii.Child("##Panel", -Vector2.One, true);
    if (!child) return;
    if (Selected != null)
    {
        DrawCharacterView(Selected);
    }
  }

  public static void DrawCharacterView(CharacterConfig? characterConfig)
  {
    if (characterConfig == null) return;
    P.OtterGuiHandler.FakeNameFileSystem.FindLeaf(characterConfig, out var l);

    var change = false;

    // IconId
    // var iconReplace = characterConfig.IconReplace;
    // if (ImGui.Checkbox("##Replace Icon Id", ref iconReplace))
    // {
    //   characterConfig.IconReplace = iconReplace;
    //   change = true;
    // }

    // if (ImGui.IsItemHovered()) ImGui.SetTooltip("Replace Icon Id");
    // ImGui.SameLine();
    // ImGui.SetCursorPosX(50);
    // var iconId = characterConfig.IconId;
    // if (ImGui.InputInt("Icon Id", ref iconId))
    // {
    //   characterConfig.IconId = iconId;
    //   change = true;
    // }

    // Name
    ImGui.SetCursorPosX(50);
    var fakeName = characterConfig.FakeNameText;
    if (ImGui.InputTextWithHint("##Name", l.Value.IncognitoName(), ref fakeName, 100))
    {
      characterConfig.FakeNameText = fakeName;
      change = true;
    }

    if (fakeName.Length != 0 && fakeName.Trim().Length == 0)
    {
      ImGui.SameLine();
      using (ImRaii.PushColor(ImGuiCol.Text, 0xf55555ff))
      {
        ImGui.TextUnformatted("Invalid Name");
      }
    }

    // FcName
    var hideFcName = characterConfig.HideFcName;
    if (ImGui.Checkbox("##FcName", ref hideFcName))
    {
      characterConfig.HideFcName = hideFcName;
      change = true;
    }

    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hide Free Company Name");
    ImGui.SameLine();
    ImGui.SetCursorPosX(50);
    var fakeFcName = characterConfig.FakeFcNameText;
    if (ImGui.InputTextWithHint("##Free Company Name", "Free Company Name", ref fakeFcName, 100))
    {
      characterConfig.FakeFcNameText = fakeFcName;
      change = true;
    }

    var enabled = characterConfig.Enabled;
    if (ImGui.Checkbox("  Enabled", ref enabled))
    {
      characterConfig.Enabled = enabled;
      change = true;
    }

    var localPlayer = Svc.ClientState.LocalPlayer;
    if (change && localPlayer != null)
    {
        if (P.NamePlate != null) P.NamePlate.ForceRedraw();
        if (P.PartyList != null) P.PartyList.ForceRedraw();
    }

    if (change && localPlayer != null && localPlayer.Name.TextValue.Equals(characterConfig.Name) && localPlayer.HomeWorld.RowId == characterConfig.World)
    {
        if (P.IpcProcessor != null) P.IpcProcessor.ChangedLocalCharacterData(characterConfig);
    }
  }
}
