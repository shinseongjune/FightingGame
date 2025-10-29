public static class FxDefaults
{
    public static string Hit(float damage, bool air)
    {
        if (air) return "FX.Hit.Air";
        if (damage < 400f) return "FX.Hit.S";
        if (damage < 600f) return "FX.Hit.M";
        return "FX.Hit.H";
    }
    public static string Guard(/*��Ʈ�ڽ� ���� �� �ʿ��ϸ� �μ� �߰�*/)
        => "FX.Block.Mid"; // �ʿ� �� High/Low�� �б�
    public static string Start() => "FX.Skill.Start";
    public static string ThrowImpact() => "FX.Throw.Impact";
}