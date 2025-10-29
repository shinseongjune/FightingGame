public static class SfxDefaults
{
    public static string Hit(float damage, bool air)
    {
        if (air) return "SFX.Hit.Air";
        if (damage < 400f) return "SFX.Hit.S";
        if (damage < 600f) return "SFX.Hit.M";
        return "SFX.Hit.H";
    }
    public static string Guard(/*상하단 필요시 인수 확장*/) => "SFX.Block.Mid";
    public static string Start() => "SFX.Skill.Start";
    public static string ThrowImpact() => "SFX.Throw.Impact";

    public static string RoundStart() => "SFX.Round.Start";
    public static string KO() => "SFX.Round.KO";
    public static string Win() => "SFX.Round.Win";
    public static string TimeUp() => "SFX.Round.TimeUp";
}
