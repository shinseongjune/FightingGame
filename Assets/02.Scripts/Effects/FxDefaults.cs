public static class FxDefaults
{
    public static string Hit(float damage, bool air)
    {
        if (air) return "FX.Hit.Air";
        if (damage < 400f) return "FX.Hit.S";
        if (damage < 600f) return "FX.Hit.M";
        return "FX.Hit.H";
    }
    public static string Guard(/*히트박스 높이 등 필요하면 인수 추가*/)
        => "FX.Block.Mid"; // 필요 시 High/Low로 분기
    public static string Start() => "FX.Skill.Start";
    public static string ThrowImpact() => "FX.Throw.Impact";
}