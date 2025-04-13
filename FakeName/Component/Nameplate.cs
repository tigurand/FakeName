using FFXIVClientStructs.Interop;
using System;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using System.Collections.Generic;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Config;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Game.ClientState.Objects.Types;

namespace FakeName.Component;

public class Nameplate : IDisposable
{
  internal Nameplate()
  {
    P.NameplateGui.OnNamePlateUpdate += OnUpdate;
    P.NameplateGui.OnPostNamePlateUpdate += OnUpdate;
    P.NameplateGui.OnDataUpdate += OnUpdate;
    P.NameplateGui.OnPostDataUpdate += OnUpdate;
    Svc.Framework.Update += FrameworkUpdate;
    ForceRedraw();
  }

  public void Dispose()
  {
    P.NameplateGui.OnNamePlateUpdate -= OnUpdate;
    P.NameplateGui.OnPostNamePlateUpdate -= OnUpdate;
    P.NameplateGui.OnDataUpdate -= OnUpdate;
    P.NameplateGui.OnPostDataUpdate -= OnUpdate;
    Svc.Framework.Update -= FrameworkUpdate;
    ForceRedraw();
  }

  public void FrameworkUpdate(IFramework _)
  {
    // P.NameplateGui.RequestRedraw();
  }

  public void ForceRedraw()
  {
    foreach (var o in new[] { UiConfigOption.NamePlateNameTitleTypeSelf, UiConfigOption.NamePlateNameTitleTypeFriend, UiConfigOption.NamePlateNameTitleTypeParty, UiConfigOption.NamePlateNameTitleTypeAlliance, UiConfigOption.NamePlateNameTitleTypeOther })
    {
      if (Svc.GameConfig.TryGet(o, out bool v))
      {
        Svc.GameConfig.Set(o, !v);
        Svc.GameConfig.Set(o, v);
      }
    }
  }

  private unsafe Character* GetOwner(IGameObject gameObject)
  {
    if (gameObject == null) return null;
    var c = (Character*)gameObject.Address;
    if (c == null || c->CompanionOwnerId == 0xE000000) return null;
    var owner = GameObjectManager.Instance()->Objects.IndexSorted[(int)gameObject.ObjectIndex - 1];
    if (owner == null || owner.Value == null) return null;
    return (Character*)owner.Value;
  }

  private unsafe void OnUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
  {
    if (!C.Enabled) return;

    foreach (var handler in handlers)
    {
      if (handler.NamePlateKind == NamePlateKind.PlayerCharacter && handler.PlayerCharacter != null)
      {
        if (P.TryGetConfig(handler.PlayerCharacter.Name.TextValue, handler.PlayerCharacter.HomeWorld.RowId, out var characterConfig))
        {
          if (characterConfig.FakeNameText.Trim().Length > 0)
          {
            handler.SetField(NamePlateStringField.Name, characterConfig.FakeNameText.Trim());
          }

          if (characterConfig.HideFcName || characterConfig.FakeFcNameText.Trim().Length > 0)
          {
            if (!Svc.Condition[ConditionFlag.BoundByDuty])
            {
              var c = (Character*)handler.PlayerCharacter.Address;
              var newFcName = characterConfig.HideFcName
                ? ""
                : characterConfig.FakeFcNameText.Trim().Length > 0
                  ? $" «{characterConfig.FakeFcNameText.Trim()}»"
                    : c->IsWanderer()
                      ? " «Wanderer»"
                      : c->IsTraveler()
                        ? " «Traveler»"
                        : c->IsVoyager()
                          ? " «Voyager»"
                          : handler.PlayerCharacter.CompanyTag.TextValue.Length > 0
                            ? $" «{handler.PlayerCharacter.CompanyTag.TextValue}»"
                            : "";
              handler.SetField(NamePlateStringField.FreeCompanyTag, newFcName);
            }
          }
        }
      }
      else if (handler.NamePlateKind == NamePlateKind.EventNpcCompanion)
      {
        var owner = GetOwner(handler.GameObject);
        if (owner == null) return;
        if (P.TryGetConfig(SeString.Parse(owner->Name).ToString(), owner->HomeWorld, out var characterConfig))
        {
          if (characterConfig.FakeNameText.Trim().Length > 0)
          {
            handler.SetField(NamePlateStringField.Title, $"《{characterConfig.FakeNameText.Trim()}》");
          }
        }
      }
      else if (handler.NamePlateKind == NamePlateKind.BattleNpcFriendly && handler.BattleChara != null)
      {
        // using GetOwner here makes it explode again? WHY??
        var owner = (IPlayerCharacter?) Svc.Objects.FirstOrDefault(t => t is IPlayerCharacter && t.EntityId == handler.BattleChara.OwnerId);
        if (owner == null) return;
        if (P.TryGetConfig(owner.Name.TextValue, owner.HomeWorld.RowId, out var characterConfig))
        {
          if (characterConfig.FakeNameText.Trim().Length > 0)
          {
            handler.SetField(NamePlateStringField.Title, $"《{characterConfig.FakeNameText.Trim()}》");
          }
        }
      }
    }
  }
}
