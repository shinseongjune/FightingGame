public static class WaitFor
{
    public static System.Collections.IEnumerator TickMasterReady()
    {
        while (TickMaster.Instance == null || !TickMaster.Instance.IsReady) yield return null;
    }
    public static System.Collections.IEnumerator PhysicsManagerReady()
    {
        while (PhysicsManager.Instance == null) yield return null;
    }
    public static System.Collections.IEnumerator BoxManagerReady()
    {
        while (BoxManager.Instance == null) yield return null;
    }
}