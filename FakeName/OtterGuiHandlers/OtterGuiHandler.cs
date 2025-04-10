using System;
using ECommons;
using FakeName.Data;
using OtterGui.Log;
using static ECommons.GenericHelpers;

namespace FakeName.OtterGuiHandlers;

public class OtterGuiHandler
{
  public FakeNameFileSystem FakeNameFileSystem;
  public Logger Logger;

  public OtterGuiHandler()
  {
    try
    {
      Logger = new();
      FakeNameFileSystem = new(this);
    }
    catch (Exception ex)
    {
      ex.Log();
    }
  }

  public void Dispose()
  {
    Safe(() => FakeNameFileSystem.Save());
  }
}
