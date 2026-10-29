using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private Behaviour[] locomotionProviders;

    public bool IsMovementEnabled { get; private set; }

    private void Awake()
    {
        SetMovementEnabled(false);
    }

    public void SetMovementEnabled(bool enabled)
    {
        IsMovementEnabled = enabled;
        if (locomotionProviders == null) { return; }

        foreach (Behaviour provider in locomotionProviders)
        {
            if (provider != null) { provider.enabled = enabled; }
        }
    }
}
