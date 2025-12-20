using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FakeName.Utils;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Runtime.InteropServices;

namespace FakeName.Component;

public class AtkTextNodeC
{
  [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
  public delegate void AtkTextNodeSetTextDelegate(IntPtr node, IntPtr text);

  [Signature("48 85 C9 0F 84 ?? ?? ?? ?? 4C 8B DC 53 56", DetourName = nameof(AtkTextNodeSetTextDetour))]
  private readonly Hook<AtkTextNodeSetTextDelegate> hook = null!;

  internal AtkTextNodeC()
  {
    Svc.Hook.InitializeFromAttributes(this);
    this.hook.Enable();
  }

  public void Dispose()
  {
    this.hook.Disable();
    this.hook.Dispose();
  }

  private void AtkTextNodeSetTextDetour(IntPtr node, IntPtr text)
  {
    try
    {
      AtkTextNodeSetText(node, text);
    }
    catch (Exception ex)
    {
      ex.Log();
      hook.Original(node, text);
    }
  }

  private unsafe void AtkTextNodeSetText(IntPtr node, IntPtr textPtr)
  {
    if (!C.Enabled)
    {
      hook.Original(node, textPtr);
      return;
    }

    var character = Svc.Objects.LocalPlayer;
    if (character == null)
    {
      TryUpdLoginUi(node, textPtr);
      return;
    }

    var charaName = character.Name.TextValue;
    var fcName = character.CompanyTag.TextValue;
    if (!P.TryGetConfig(charaName, character.HomeWorld.RowId, out var characterConfig))
    {
      hook.Original(node, textPtr);
      return;
    }

    if (characterConfig.FakeFcNameText.Trim().Length == 0
     || characterConfig.FakeNameText.Trim().Length == 0)
    {
      hook.Original(node, textPtr);
      return;
    }

    var text = SeStringUtils.ReadRawSeString(textPtr);
    bool changed = false;
    foreach (var payload in text.Payloads)
    {
      switch (payload)
      {
        /*case PlayerPayload pp:
          if (pp.PlayerName.Contains(charaName)) {
            pp.PlayerName = pp.PlayerName.Replace(charaName, characterConfig.FakeNameText.Trim());
          }
          break;*/
        case TextPayload txt:
          if (txt.Text == null) { }
          else if (txt.Text.Equals(charaName))
          {
            txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText.Trim());
            changed = true;
          }
          /*else if (txt.Text.Contains($"\n《{charaName}》"))
          {
            Service.Log.Debug($"包含角色名的文本:{txt.Text}");
            txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText.Trim());
            changed = true;
          }*/
          else if (txt.Text.Equals($"«{fcName}»"))
          {
            txt.Text = txt.Text.Replace(fcName, characterConfig.FakeFcNameText.Trim());
            changed = true;
          }
          else if (txt.Text.Equals($" «{fcName}»"))
          {
            txt.Text = txt.Text.Replace(fcName, characterConfig.FakeFcNameText.Trim());
            changed = true;
          }
          else if (txt.Text.Equals($" [{fcName}]"))
          {
            txt.Text = txt.Text.Replace(fcName, characterConfig.FakeFcNameText.Trim());
            changed = true;
          }
          // else if (txt.Text.Equals($"{charaName} «{fcName}»"))
          // {
          //   txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText.Trim())
          //                 .Replace(fcName, characterConfig.FakeFcNameText.Trim());
          // }
          // else if (txt.Text.Contains($"级 {charaName}"))
          // {
          //    txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText.Trim());
          // }
          else if (txt.Text.Contains(charaName))
          {
            // Service.Log.Debug($"包含角色名的文本:{txt.Text}");
          }
          break;
      }
    }

    if (changed)
    {
      fixed (byte* newText = text.Encode().Terminate())
      {
        hook.Original(node, (IntPtr)newText);
      }
    }
    else
    {
      hook.Original(node, textPtr);
    }
  }

  private unsafe void TryUpdLoginUi(IntPtr node, IntPtr textPtr)
  {
    var agent = AgentLobby.Instance();
    if (agent == null)
    {
      hook.Original(node, textPtr);
      return;
    }

    if (!C.TryGetWorldDic(agent->WorldId, out var worldDic))
    {
      hook.Original(node, textPtr);
      return;
    }

    var text = SeStringUtils.ReadRawSeString(textPtr);
    bool changed = false;
    foreach (var pair in worldDic)
    {
      var charaName = pair.Key;
      var characterConfig = pair.Value;

      if (characterConfig.FakeNameText.Length == 0 || !characterConfig.Enabled)
      {
        continue;
      }

      foreach (var payload in text.Payloads)
      {
        switch (payload)
        {
          /*case PlayerPayload pp:
            if (pp.PlayerName.Contains(charaName)) {
                pp.PlayerName = pp.PlayerName.Replace(charaName, characterConfig.FakeNameText);
            }
            break;*/
          case TextPayload txt:
            if (txt.Text != null && txt.Text.Equals(charaName))
            {
              // Service.Log.Debug($"world[{agent->WorldId}] 替换{txt.Text} {charaName}->{characterConfig.FakeNameText}");
              txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText);
              changed = true;
            }
            else if (txt.Text != null && txt.Text.Equals($"要以{charaName}登录吗？"))
            {
              txt.Text = txt.Text.Replace(charaName, characterConfig.FakeNameText);
              changed = true;
            }
            else if (txt.Text != null && txt.Text.Contains(charaName))
            {
              // Service.Log.Verbose($"包含角色名的文本:{txt.Text}");
            }
            break;
        }
      }
    }

    if (changed)
    {
      fixed (byte* newText = text.Encode().Terminate())
      {
        hook.Original(node, (IntPtr)newText);
      }
    }
    else
    {
      hook.Original(node, textPtr);
    }
  }
}
