using UnityEngine;

public class CharacterFSM : MonoBehaviour, ITicker
{
    private CharacterState current;
    public CharacterState CurrentState => current;

    public void Tick()
    {
        current?.OnUpdate();
    }

    public void TransitionTo(CharacterState next)
    {
        if (next == null) return;

        Debug.Log($"[FSM] {current?.GetType().Name} ¡æ {next.GetType().Name}");

        current?.OnExit();
        current = next;
        current?.OnEnter();
    }
}
