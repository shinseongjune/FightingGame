using System.Collections.Generic;

public interface ISelectSceneModel
{
    IReadOnlyList<SelectableItemViewData> Characters { get; }
    IReadOnlyList<SelectableItemViewData> Stages { get; }

    bool IsUnlocked(string id);
    bool IsHidden(string id);
}
