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
        // ���� ���� �켱���� Provider�� �������� ���
        for (int i = 0; i < providers.Count; i++)
        {
            var d = providers[i].p.GetSnapshot();
            // �ʿ� �� '���Է�' �Ǵ� ������ ���� �� ����(���⼱ �״�� ��ȯ)
            return d;
        }
        return default; // Provider ������ ���Է�
    }
}