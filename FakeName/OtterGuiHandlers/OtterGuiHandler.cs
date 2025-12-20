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
    Logger = new();

    try
    {
      FakeNameFileSystem = new FakeNameFileSystem(this);
    }
    catch (Exception ex)
    {
      ex.Log();
      throw;
    }
  }

  public void Dispose()
  {
    Safe(() => FakeNameFileSystem.Save());
  }
}
