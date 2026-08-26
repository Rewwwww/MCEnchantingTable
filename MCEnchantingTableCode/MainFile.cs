using System.Reflection;
using BaseLib.Config;
using MCEnchantingTable.MCEnchantingTableCode.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MCEnchantingTable.MCEnchantingTableCode;

//You're recommended but not required to keep all your code in this package and all your assets in the MCEnchantingTable folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MCEnchantingTable"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            Logger.Info("MCEnchantingTable Content is already initialized; skipping duplicate initialization.");
            return;
        }

        _initialized = true;
        ModConfigRegistry.Register(ModId, new GameplaySettings());

        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
     
        Harmony harmony = new(ModId);

        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Logger.Info("MCEnchantingTable Content initialized from " + Assembly.GetExecutingAssembly().Location);
    }
}
