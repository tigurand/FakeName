using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace FakeName
{
    internal class Service
    {
        [PluginService]
        internal static INamePlateGui NamePlateGui { get; private set; } = null!;

        internal static void Initialize(Dalamud.Plugin.IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();
        }
    }
}