using System.Collections.Generic;

public sealed class InputArbiter
{
    private readonly List<(IInputProvider p, int priority)> providers = new();

    public void Register(IInputProvider p, int priority = 0)
    {
        providers.Add((p, priority));
        providers.Sort((a, b) => b.priority.CompareTo(a.priority));
    }

    public void Unregister(IInputProvider p)
    {
        providers.RemoveAll(x => x.p == p);
    }

    public InputData Resolve()
    {
        // 가장 높은 우선순위 Provider의 스냅샷을 사용
        for (int i = 0; i < providers.Count; i++)
        {
            var d = providers[i].p.GetSnapshot();
            // 필요 시 '무입력' 판단 로직을 넣을 수 있음(여기선 그대로 반환)
            return d;
        }
        return default; // Provider 없으면 무입력
    }
}