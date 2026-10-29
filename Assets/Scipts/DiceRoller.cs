using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DiceRoller : MonoBehaviour
{
    public static UnityAction<int> OnDiceRoll;

    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float hudDistance = 1.5f;
    [SerializeField] private float hudHorizontalOffset;
    [SerializeField] private float hudVerticalOffset = -0.12f;

    private void Awake()
    {
        if (playerCamera == null) { playerCamera = Camera.main; }
        if (turnManager == null) { turnManager = FindAnyObjectByType<TurnManager>(); }
        if (turnManager == null) { turnManager = CreateGameplayServices(); }
        turnManager.ConfigureHud(playerCamera, hudDistance, hudHorizontalOffset, hudVerticalOffset);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) { Roll(); }
    }

    public void Roll()
    {
        if (turnManager != null) { turnManager.RollPlayerDice(); }
    }

    private static TurnManager CreateGameplayServices()
    {
        GameObject gameplaySystems = new GameObject("Gameplay Systems");
        gameplaySystems.AddComponent<BoardManager>();
        gameplaySystems.AddComponent<TurnTimer>();
        return gameplaySystems.AddComponent<TurnManager>();
    }
}