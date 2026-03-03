using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized panel management. Ensures only one panel is open at a time.
/// Panels register themselves on Awake, then call PanelManager.Open(this)
/// which closes everything else first.
/// Attach to a persistent object (GameManager or Canvas).
/// </summary>
public class PanelManager : MonoBehaviour
{
    readonly List<IPanel> panels = new();

    void Awake()
    {
        Services.Register(this);
    }

    public void Register(IPanel panel)
    {
        if (!panels.Contains(panel))
            panels.Add(panel);
    }

    public void Open(IPanel panel)
    {
        foreach (var p in panels)
        {
            if (p != panel)
                p.Close();
        }
    }

    public void CloseAll()
    {
        foreach (var p in panels)
            p.Close();
    }
}

public interface IPanel
{
    void Close();
}
