// SimpleSelectSceneModel.cs
using System.Collections.Generic;
using System.Linq;

public class SimpleSelectSceneModel : ISelectSceneModel
{
    public IReadOnlyList<SelectableItemViewData> Characters { get; private set; }
    public IReadOnlyList<SelectableItemViewData> Stages { get; private set; }

    private readonly HashSet<string> _locked = new();
    private readonly HashSet<string> _hidden = new();

    public SimpleSelectSceneModel(
        IEnumerable<SelectableItemViewData> characters,
        IEnumerable<SelectableItemViewData> stages,
        IEnumerable<string> lockedIds = null,
        IEnumerable<string> hiddenIds = null)
    {
        Characters = characters.ToList();
        Stages = stages.ToList();
        if (lockedIds != null) foreach (var id in lockedIds) _locked.Add(id);
        if (hiddenIds != null) foreach (var id in hiddenIds) _hidden.Add(id);
    }

    public bool IsUnlocked(string id) => !_locked.Contains(id);
    public bool IsHidden(string id) => _hidden.Contains(id);
}
