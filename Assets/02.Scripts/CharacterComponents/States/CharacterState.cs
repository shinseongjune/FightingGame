using UnityEngine;

public abstract class CharacterState
{
    protected CharacterFSM fsm;
    protected GameObject owner;

    public CharacterState(CharacterFSM fsm)
    {
        this.fsm = fsm;
        this.owner = fsm.gameObject;
    }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void OnUpdate() { }
}
