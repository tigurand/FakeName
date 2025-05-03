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
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;

namespace FakeName.Component;

public class NamePlate : IDisposable
{
  internal NamePlate()
  {
    P.NamePlateGui.OnNamePlateUpdate += OnUpdate;
    ForceRedraw();
  }

  public void Dispose()
  {
    P.NamePlateGui.OnNamePlateUpdate -= OnUpdate;
    ForceRedraw();
  }

  public void ForceRedraw()
  {
    P.NamePlateGui.RequestRedraw();
  }

  private unsafe void OnUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
  {
    if (!C.Enabled) return;

    foreach (var handler in handlers)
    {
      // Players
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
      // Minions
      else if (handler.NamePlateKind == NamePlateKind.EventNpcCompanion && handler.GameObject != null && handler.GameObject.ObjectKind == ObjectKind.Companion)
      {
        var c = (Character*)handler.GameObject.Address;
        if (c == null || c->CompanionOwnerId == 0xE000000 || c->FateId != 0) return;
        var owner = (IPlayerCharacter?) Svc.Objects.FirstOrDefault(t => t is IPlayerCharacter && t.EntityId == c->CompanionOwnerId);
        if (P.TryGetConfig(owner.Name.TextValue, owner.HomeWorld.RowId, out var characterConfig))
        {
          if (characterConfig.FakeNameText.Trim().Length > 0)
          {
            handler.SetField(NamePlateStringField.Title, $"《{characterConfig.FakeNameText.Trim()}》");
          }
        }
      }
      // Pets (Eos, Carbuncle, ...)
      else if (handler.NamePlateKind == NamePlateKind.BattleNpcFriendly && handler.GameObject != null && handler.GameObject.SubKind == 2)
      {
        var owner = (IPlayerCharacter?) Svc.Objects.FirstOrDefault(t => t is IPlayerCharacter && t.EntityId == handler.GameObject.OwnerId);
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
