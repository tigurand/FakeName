using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using System.Collections.Generic;

namespace FakeName.Hook;

public class ChatMessage : IDisposable
{
  internal ChatMessage()
  {
    Svc.Chat.ChatMessage += OnChatMessage;
  }

  public void Dispose()
  {
    Svc.Chat.ChatMessage -= OnChatMessage;
  }

  private void OnChatMessage(
    XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
  {
    ChangeNames(sender);
    ChangeNames(message);
  }

  private void ChangeNames(SeString text)
  {
    if (!C.Enabled) return;

    var character = Svc.ClientState.LocalPlayer;
    if (character == null) return;

    var localCharaName = character.Name.TextValue;
    var localCharaReplace = localCharaName;
    if (P.TryGetConfig(localCharaName, character.HomeWorld.RowId, out var localCharacterConfig))
    {
      localCharaReplace = localCharacterConfig.FakeNameText;
    }

    // Svc.Log.Debug($"{text.ToString()}");
    bool setNextTextPayload = false;
    string nextTextPayloadText = "";
    for (int i = 0; i < text.Payloads.Count; i++) {
      var p = text.Payloads[i];
      if (p.Type == PayloadType.Player) {
        var playerPayload = (PlayerPayload)p;
        // Svc.Log.Debug($"[PLAYER]    - {i.ToString()}: {p.ToString()}");
        if (P.TryGetConfig(playerPayload.PlayerName, playerPayload.World.RowId, out var characterConfig)) {
          nextTextPayloadText = characterConfig.FakeNameText;
          setNextTextPayload = true;
        }
      } else if (p.Type == PayloadType.RawText) {
        var textPayload = (TextPayload)p;
        if (setNextTextPayload) {
          textPayload.Text = nextTextPayloadText;
          setNextTextPayload = false;
        } else if (textPayload.Text == localCharaName) {
          textPayload.Text = localCharaReplace;
        }
      }
      // Svc.Log.Debug($"    - {i.ToString()}: {p.ToString()}");
    }
  }
}
