using BepInEx.Configuration;

namespace CalculateScrapForQuota;

public class Config
{
    public readonly ConfigEntry<bool> isVerbose;

    public Config(ConfigFile cfg)
    {
        isVerbose = cfg.Bind(
            "General.Debug",
            "isVerbose",
            false,
            "To display plugin logs in console."
        );
    }
}