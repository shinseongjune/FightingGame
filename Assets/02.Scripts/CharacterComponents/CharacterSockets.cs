using System;
using UnityEngine;

[Serializable]
public struct Socket
{
    public string name;
    public Transform tr;
}

public class CharacterSockets : MonoBehaviour
{
    public Socket[] sockets;

    public Transform Find(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < sockets.Length; ++i)
        {
            if (sockets[i].tr != null && string.Equals(sockets[i].name, name, StringComparison.Ordinal))
                return sockets[i].tr;
        }
        return null;
    }
}
