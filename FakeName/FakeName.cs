using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.SimpleGui;
using FakeName.Component;
using FakeName.Data;
using FakeName.Gui;
using FakeName.OtterGuiHandlers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FakeName;

public class FakeName : IDalamudPlugin
{
  public static FakeName P;
  public static Config C => P.NewConfig;
  public static IpcDataManager Idm => P.IpcDataManager;
  public INamePlateGui NamePlateGui { get; private set; } = null!;

  public PluginConfig Config;
  public Config NewConfig;
  public IpcDataManager IpcDataManager;

  public OtterGuiHandler OtterGuiHandler;

  public AtkTextNodeC AtkTextNodeC;
  public ChatMessage ChatMessage;
  public NamePlate NamePlate;
  public PartyList PartyList;
  public TargetListInfo TargetListInfo;

  public IpcProcessor IpcProcessor;

  public string msg = "null";

  public FakeName(IDalamudPluginInterface pi)
  {
    P = this;
    ECommonsMain.Init(pi, this);
    Service.Initialize(pi);

    Config = Svc.PluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
    NewConfig = EzConfig.Init<Config>();
    IpcDataManager = new();

    Svc.PluginInterface.UiBuilder.OpenMainUi += EzConfigGui.Open;
    EzConfigGui.Init(UI.Draw);
    EzCmd.Add("/fakename", EzConfigGui.Open, "Open FakeName Configuration");
    EzCmd.Add("/fn", EzConfigGui.Open, "Alias for /fakename");

    OldConfigMove(Config, NewConfig);

    Svc.Framework.RunOnFrameworkThread(() =>
    {
      OtterGuiHandler = new();
      RepairFileSystem();
      AtkTextNodeC = new();
      ChatMessage = new();
      NamePlate = new();
      PartyList = new();
      TargetListInfo = new();
      IpcProcessor = new();
    });
  }

  public void OldConfigMove(PluginConfig oldConfig, Config newConfig)
  {
    if (oldConfig.WorldCharacterDictionary.Count > 0)
    {
      foreach (var (worldId, characters) in P.Config.WorldCharacterDictionary.ToArray())
      {
        foreach (var (name, characterConfig) in characters.ToArray())
        {
          C.TryAddCharacter(name, worldId, characterConfig);
          characters.Remove(name);
          if (characters.Count == 0)
          {
            P.Config.WorldCharacterDictionary.Remove(worldId);
          }
        }
      }
    }

    foreach (var (_, characters) in C.WorldCharacterDictionary.ToArray())
    {
      foreach (var (_, characterConfig) in characters.ToArray())
      {
        C.Characters.Add(characterConfig);
      }
    }
  }

  public void RepairFileSystem()
  {
    foreach (var characterConfig in C.Characters)
    {
      var fs = P.OtterGuiHandler.FakeNameFileSystem;

      if (!fs.FindLeaf(characterConfig, out var leaf))
      {
        fs.CreateLeaf(fs.Root, fs.ConvertToName(characterConfig), characterConfig);
        PluginLog.Debug($"CreateLeaf {characterConfig.Name}({characterConfig.World}) {leaf == null}");
      }
    }
  }

  public void Dispose()
  {
    Safe(() => IpcProcessor?.Dispose());

    Safe(() => AtkTextNodeC?.Dispose());
    Safe(() => ChatMessage?.Dispose());
    Safe(() => NamePlate?.Dispose());
    Safe(() => PartyList?.Dispose());
    Safe(() => TargetListInfo?.Dispose());

    Safe(() => OtterGuiHandler?.Dispose());

    Svc.PluginInterface.UiBuilder.OpenMainUi -= EzConfigGui.Open;
    ECommonsMain.Dispose();
    P = null;
  }

  public static string IncognitoModeName(string name)
  {
    if (!C.IncognitoMode)
    {
      return name;
    }
    else
    {
      return name.Substring(0, 1) + "...";
    }
  }

  public bool TryGetConfig(string name, uint world, [MaybeNullWhen(false)] out CharacterConfig characterConfig, bool ignoreEnabled = false)
  {
    if (Idm.TryGetCharacterConfig(name, world, out characterConfig))
    {
      // PluginLog.Debug($"找到了{characterConfig.Name}的ipc配置：");
      return ignoreEnabled ? true : characterConfig.Enabled;
    }

    if (C.TryGetCharacterConfig(name, world, out characterConfig))
    {
      return ignoreEnabled ? true : characterConfig.Enabled;
    }

    return false;
  }
}
