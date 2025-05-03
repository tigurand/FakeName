using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FakeName.Utils;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FakeName.Component;

public class PartyList : IDisposable
{
  private readonly Dictionary<uint, string> modifiedNamePlates = new();
  private DateTime lastUpdate = DateTime.Today;
  public PartyList()
  {
    Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", OnPartyListUpdate);
    Svc.Framework.Update += OnUpdate;
    ForceRedraw();
  }

  public void Dispose()
  {
    Svc.AddonLifecycle.UnregisterListener(OnPartyListUpdate);
    Svc.Framework.Update -= OnUpdate;
    RefreshPartyList(true);
  }

  public void ForceRedraw()
  {
    RefreshPartyList(true);
    RefreshPartyList();
  }

  private void OnUpdate(IFramework framework)
  {
    try
    {
      if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(5))
      {
        RefreshPartyList();
        lastUpdate = DateTime.Now;
      }
    }
    catch (Exception e)
    {
      e.Log();
    }
  }

  private void OnPartyListUpdate(AddonEvent type, AddonArgs args)
  {
    try
    {
      if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(1))
      {
        RefreshPartyList();
        lastUpdate = DateTime.Now;
      }
    }
    catch (Exception e)
    {
      e.Log();
    }
  }

  public unsafe void RefreshPartyList(bool dispose = false)
  {
    var localPlayer = Svc.ClientState.LocalPlayer;
    if (localPlayer == null)
    {
      return;
    }

    var partyListMemberStructs = GetPartyListAddon();
    var cwProxy = InfoProxyCrossRealm.Instance();
    foreach (var memberStruct in partyListMemberStructs)
    {
      var nodeText = memberStruct.Name->NodeText.ToString();
      var nameNode = memberStruct.Name;
      var match = Regex.Match(nodeText, @"[\u0000-\u001F\u007F-\u009F\uE000-\uF8FF\s]*([A-Za-z'\-]+(?:\s+[A-Za-z'\-]+)*)[\u0000-\u001F\u007F-\u009F\uE000-\uF8FF\s]*$");
      if (match.Success)
      {
        var currentName = match.Groups[1].Value.Trim();
        ReplaceName(nameNode, currentName, localPlayer.Name.TextValue, localPlayer.HomeWorld.RowId, dispose);
        if (Svc.Party.Any())
        {
          ReplacePartyListHud(currentName, nameNode, dispose);
        }
        else
        {
          ReplaceCrossPartyListHud(currentName, nameNode, cwProxy, dispose);
        }
      }
    }
  }

  private uint idx(string name, uint world)
  {
    unchecked
    {
      int hash = name.GetHashCode();
      hash = (hash * 397) ^ (int)world;
      return (uint)hash;
    }
  }

  private unsafe bool ReplaceName(AtkTextNode* nameNode, string currentName, string playerName, uint world, bool dispose = false)
  {
    var configExists = P.TryGetConfig(playerName, world, out var characterConfig, true);

    var index = idx(playerName, world);

    if (configExists && playerName.Equals(currentName) && characterConfig.Enabled && C.Enabled && !dispose)
    {
      if (characterConfig.FakeNameText.Trim().Length == 0) return true;
      modifiedNamePlates[index] = characterConfig.FakeNameText.Trim();
      nameNode->NodeText.SetString(nameNode->NodeText.ToString().Replace(playerName, characterConfig.FakeNameText.Trim()));
      return true;
    }
    else
    {
      if (modifiedNamePlates.TryGetValue(index, out var old))
      {
        if (currentName.Equals(old))
        {
          if (!configExists || !characterConfig.Enabled || !C.Enabled || dispose)
          {
            nameNode->NodeText.SetString(nameNode->NodeText.ToString().Replace(old, playerName));
            modifiedNamePlates.Remove(index);
          }
        }
      }
    }
    return false;
  }

  public unsafe void ReplacePartyListHud(string currentName, AtkTextNode* nameNode, bool dispose)
  {
    foreach (var partyMember in Svc.Party)
    {
      if (ReplaceName(nameNode, currentName, partyMember.Name.TextValue, partyMember.World.RowId, dispose))
      {
        break;
      }
    }
  }

  public unsafe void ReplaceCrossPartyListHud(string currentName, AtkTextNode* nameNode, InfoProxyCrossRealm* cwProxy, bool dispose)
  {
    var localIndex = cwProxy->LocalPlayerGroupIndex;
    var crossRealmGroup = cwProxy->CrossRealmGroups[localIndex];

    for (var i = 0; i < crossRealmGroup.GroupMemberCount; i++)
    {
      var groupMember = crossRealmGroup.GroupMembers[i];
      if (ReplaceName(nameNode, currentName, groupMember.NameString, (ushort)groupMember.HomeWorld, dispose))
      {
        break;
      }
    }
  }

  private unsafe List<AddonPartyList.PartyListMemberStruct> GetPartyListAddon()
  {
    var partyListAddon = (AddonPartyList*) Svc.GameGui.GetAddonByName("_PartyList", 1);

    List<AddonPartyList.PartyListMemberStruct> p = [
      partyListAddon->PartyMembers[0],
      partyListAddon->PartyMembers[1],
      partyListAddon->PartyMembers[2],
      partyListAddon->PartyMembers[3],
      partyListAddon->PartyMembers[4],
      partyListAddon->PartyMembers[5],
      partyListAddon->PartyMembers[6],
      partyListAddon->PartyMembers[7]
    ];

    return p.Where(n => n.Name->NodeText.ToString().Length > 0).ToList();
  }

  private unsafe void TryCleanUp(uint idx, string fakeNameText, AtkTextNode* nameNode)
  {
    if (!modifiedNamePlates.TryGetValue(idx, out var old))
    {
      return;
    }

    Svc.Log.Debug($"renaming {fakeNameText} to {old}");
    nameNode->NodeText.SetString(nameNode->NodeText.ToString().Replace(fakeNameText, old));
    modifiedNamePlates.Remove(idx);
  }
}
