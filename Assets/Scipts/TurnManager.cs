using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public enum TurnState { PlayerTurn, PlayerNavigating, Resolving, AwaitingDecision, BotTurn, GameOver }

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI diceText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button passButton;
    [SerializeField] private int startingCash = 1500;

    private readonly List<int> playerProperties = new List<int>();
    private readonly List<int> botProperties = new List<int>();
    private BotPawn botPawn;
    private int playerCash;
    private int botCash;
    private int playerTileIndex;
    private int targetTile = -1;
    private int spacesToMove;
    private bool passedGo;
    private Camera playerCamera;
    private float hudDistance = 1.5f;
    private float hudHorizontalOffset;
    private float hudVerticalOffset = -0.12f;

    public TurnState currentState { get; private set; } = TurnState.PlayerTurn;
    public int currentTile => boardManager != null ? boardManager.currentTile : -1;
    public int TargetTile => targetTile;
    public bool CanRoll => currentState == TurnState.PlayerTurn;

    public void ConfigureHud(Camera camera, float distance, float horizontalOffset, float verticalOffset)
    {
        if (camera != null) { playerCamera = camera; }
        hudDistance = distance;
        hudHorizontalOffset = horizontalOffset;
        hudVerticalOffset = verticalOffset;
    }

    private void Awake()
    {
        if (boardManager == null) { boardManager = FindAnyObjectByType<BoardManager>(); }
        if (boardManager == null) { boardManager = new GameObject("Board Manager").AddComponent<BoardManager>(); }
    }

    private void Start()
    {
        playerCash = startingCash;
        botCash = startingCash;
        CreateEventSystem();
        CreateWorldHud();
        CreateBotPawn();
        playerTileIndex = boardManager.currentTile >= 0 ? boardManager.currentTile : 0;
        UpdateEconomyText();
        SetStatus("YOUR TURN  |  ROLL, THEN WALK THE LIT PATH");
        SetButtons(true, false, false);
    }

    private void Update()
    {
        if (currentState == TurnState.PlayerNavigating && boardManager.IsPlayerOnTile(targetTile)) { CompletePlayerMove(); }
    }

    public void RollPlayerDice()
    {
        if (!CanRoll || boardManager == null || boardManager.TileCount < 2) { return; }
        if (boardManager.currentTile < 0)
        {
            SetStatus("STEP CLOSER TO A BOARD TILE BEFORE ROLLING");
            return;
        }

        playerTileIndex = boardManager.currentTile;
        int firstDie = Random.Range(1, 7);
        int secondDie = Random.Range(1, 7);
        DiceRoller.OnDiceRoll?.Invoke(2);
        Dice.OnDiceResult?.Invoke(0, firstDie);
        Dice.OnDiceResult?.Invoke(1, secondDie);
        spacesToMove = firstDie + secondDie;
        targetTile = (playerTileIndex + spacesToMove) % boardManager.TileCount;
        passedGo = playerTileIndex + spacesToMove >= boardManager.TileCount;
        currentState = TurnState.PlayerNavigating;
        boardManager.HighlightPath(playerTileIndex, spacesToMove);
        if (diceText != null) { diceText.text = $"{firstDie} + {secondDie} = {spacesToMove}"; }
        SetStatus($"WALK TO TILE {targetTile + 1}  |  {spacesToMove} SPACES LIT");
        SetButtons(false, false, false);
    }

    public void BuyCurrentProperty()
    {
        if (currentState != TurnState.AwaitingDecision) { return; }
        BoardTileData tile = boardManager.GetTileData(targetTile);
        if (tile == null || !tile.IsPurchasable || tile.Owner != PropertyOwner.None || playerCash < tile.Price) { return; }

        tile.SetOwner(PropertyOwner.Player);
        playerCash -= tile.Price;
        playerProperties.Add(targetTile);
        SetStatus($"BOUGHT {tile.DisplayName} FOR ${tile.Price}");
        UpdateEconomyText();
        StartCoroutine(EndPlayerTurnAfterPause());
    }

    public void PassCurrentProperty()
    {
        if (currentState != TurnState.AwaitingDecision) { return; }
        SetStatus("PROPERTY PASSED");
        StartCoroutine(EndPlayerTurnAfterPause());
    }

    private void CompletePlayerMove()
    {
        currentState = TurnState.Resolving;
        playerTileIndex = targetTile;
        boardManager.ClearHighlights();
        if (passedGo) { playerCash += 200; }
        ResolveLanding(PropertyOwner.Player, targetTile);
        if (currentState != TurnState.AwaitingDecision && currentState != TurnState.GameOver)
        {
            UpdateEconomyText();
            StartCoroutine(EndPlayerTurnAfterPause());
        }
    }

    private void ResolveLanding(PropertyOwner visitor, int tileIndex)
    {
        BoardTileData tile = boardManager.GetTileData(tileIndex);
        if (tile == null) { return; }

        if (tile.IsPurchasable)
        {
            if (tile.Owner == PropertyOwner.None)
            {
                if (visitor == PropertyOwner.Player && playerCash >= tile.Price)
                {
                    currentState = TurnState.AwaitingDecision;
                    SetStatus($"{tile.DisplayName}  |  BUY FOR ${tile.Price}  |  RENT ${tile.Rent}");
                    SetButtons(false, true, true);
                    UpdateEconomyText();
                    return;
                }

                if (visitor == PropertyOwner.Bot && botCash >= tile.Price)
                {
                    tile.SetOwner(PropertyOwner.Bot);
                    botCash -= tile.Price;
                    botProperties.Add(tileIndex);
                    SetStatus($"BOT BOUGHT {tile.DisplayName} FOR ${tile.Price}");
                }
                else { SetStatus($"{tile.DisplayName} IS UNOWNED"); }
            }
            else if (tile.Owner != visitor)
            {
                TransferRent(visitor, tile);
                SetStatus($"${tile.Rent} RENT PAID FOR {tile.DisplayName}");
            }
            else { SetStatus($"YOUR {tile.DisplayName}"); }
        }
        else if (tile.Type == BoardTileType.Go) { SetStatus("LANDED ON GO"); }
        else if (tile.Type == BoardTileType.Tax)
        {
            ChangeCash(visitor, -100);
            SetStatus("TAX PAID: $100");
        }
        else if (tile.Type == BoardTileType.Chance)
        {
            int change = Random.Range(0, 2) == 0 ? -50 : 100;
            ChangeCash(visitor, change);
            SetStatus(change > 0 ? "CHANCE: COLLECT $100" : "CHANCE: PAY $50");
        }
        else if (tile.Type == BoardTileType.GoToJail)
        {
            ChangeCash(visitor, -50);
            SetStatus("GO TO JAIL: $50 FEE  |  WALK TO TILE 11 NEXT TURN");
        }
        else { SetStatus(tile.Type == BoardTileType.FreeParking ? "FREE PARKING" : "JAIL VISITATION"); }

        UpdateEconomyText();
        CheckForBankruptcy();
    }

    private void TransferRent(PropertyOwner visitor, BoardTileData tile)
    {
        if (visitor == PropertyOwner.Player)
        {
            playerCash -= tile.Rent;
            botCash += tile.Rent;
        }
        else
        {
            botCash -= tile.Rent;
            playerCash += tile.Rent;
        }
    }

    private void ChangeCash(PropertyOwner owner, int amount)
    {
        if (owner == PropertyOwner.Player) { playerCash += amount; }
        else { botCash += amount; }
    }

    private IEnumerator EndPlayerTurnAfterPause()
    {
        SetButtons(false, false, false);
        yield return new WaitForSeconds(1f);
        if (CheckForBankruptcy()) { yield break; }
        StartCoroutine(PlayBotTurn());
    }

    private IEnumerator PlayBotTurn()
    {
        currentState = TurnState.BotTurn;
        SetStatus("BOT TURN  |  ROLLING...");
        SetButtons(false, false, false);
        yield return new WaitForSeconds(0.8f);

        int firstDie = Random.Range(1, 7);
        int secondDie = Random.Range(1, 7);
        int spaces = firstDie + secondDie;
        int previousTile = botPawn.CurrentTileIndex;
        int destination = (previousTile + spaces) % boardManager.TileCount;
        if (previousTile + spaces >= boardManager.TileCount) { botCash += 200; }
        if (diceText != null) { diceText.text = $"{firstDie} + {secondDie} = {spaces}"; }
        SetStatus($"BOT WALKS {spaces} SPACES");
        yield return StartCoroutine(botPawn.Move(boardManager, spaces, 0.22f));

        currentState = TurnState.Resolving;
        ResolveLanding(PropertyOwner.Bot, destination);
        if (currentState == TurnState.GameOver) { yield break; }
        yield return new WaitForSeconds(1f);
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn()
    {
        currentState = TurnState.PlayerTurn;
        SetStatus("YOUR TURN  |  ROLL, THEN WALK THE LIT PATH");
        SetButtons(true, false, false);
        UpdateEconomyText();
    }

    private bool CheckForBankruptcy()
    {
        if (playerCash > 0 && botCash > 0) { return false; }
        currentState = TurnState.GameOver;
        SetButtons(false, false, false);
        SetStatus(playerCash <= 0 ? "BANKRUPT  |  BOT WINS" : "BOT BANKRUPT  |  YOU WIN");
        return true;
    }

    private void UpdateEconomyText()
    {
        if (moneyText != null)
        {
            string playerOwned = GetOwnedNames(playerProperties);
            string botOwned = GetOwnedNames(botProperties);
            moneyText.text = $"YOU  ${playerCash}: {playerOwned}\nBOT  ${botCash}: {botOwned}";
        }
    }

    private string GetOwnedNames(List<int> tileIndices)
    {
        if (tileIndices.Count == 0) { return "no properties"; }
        List<string> names = new List<string>();
        foreach (int tileIndex in tileIndices)
        {
            BoardTileData tile = boardManager.GetTileData(tileIndex);
            if (tile != null) { names.Add(tile.DisplayName); }
        }
        return string.Join(", ", names);
    }

    private void SetButtons(bool canRoll, bool canBuy, bool canPass)
    {
        if (rollButton != null) { rollButton.interactable = canRoll; }
        if (buyButton != null) { buyButton.interactable = canBuy; }
        if (passButton != null) { passButton.interactable = canPass; }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) { statusText.text = message; }
    }

    private void CreateBotPawn()
    {
        GameObject botObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        botObject.name = "Bot Pawn";
        botObject.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
        Renderer pawnRenderer = botObject.GetComponent<Renderer>();
        if (pawnRenderer != null) { pawnRenderer.material.color = new Color(0.9f, 0.25f, 0.2f); }
        botPawn = botObject.AddComponent<BotPawn>();
        botPawn.PlaceOnTile(boardManager, 0);
    }

    private void CreateEventSystem()
    {
        if (EventSystem.current == null) { new GameObject("EventSystem", typeof(EventSystem)); }
    }

    private void CreateWorldHud()
    {
        if (playerCamera == null) { playerCamera = Camera.main; }
        if (playerCamera == null)
        {
            Debug.LogError("Assign the existing XR Main Camera to DiceRoller to position the Monopoly HUD.", this);
            return;
        }

        GameObject canvasObject = new GameObject("Monopoly World HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(760f, 400f);
        canvasRect.SetParent(playerCamera.transform, false);
        canvasRect.localPosition = new Vector3(hudHorizontalOffset, hudVerticalOffset, hudDistance);
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * 0.0016f;

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasRect, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(0.035f, 0.075f, 0.07f, 0.98f);

        statusText = CreateHudText(panelRect, "TurnStatus", new Vector2(0.5f, 0.88f), new Vector2(0.9f, 0.2f), 27, TextAlignmentOptions.Center);
        diceText = CreateHudText(panelRect, "DiceResult", new Vector2(0.5f, 0.62f), new Vector2(0.88f, 0.2f), 52, TextAlignmentOptions.Center);
        diceText.fontStyle = FontStyles.Bold;
        diceText.color = new Color(1f, 0.82f, 0.32f);
        moneyText = CreateHudText(panelRect, "Economy", new Vector2(0.5f, 0.38f), new Vector2(0.86f, 0.22f), 24, TextAlignmentOptions.Center);
        rollButton = CreateHudButton(panelRect, "ROLL", new Vector2(-220f, 48f), RollPlayerDice);
        buyButton = CreateHudButton(panelRect, "BUY", new Vector2(0f, 48f), BuyCurrentProperty);
        passButton = CreateHudButton(panelRect, "PASS", new Vector2(220f, 48f), PassCurrentProperty);
        canvasObject.AddComponent<WorldSpaceUIRaycaster>().Configure(canvas);
    }

    private TextMeshProUGUI CreateHudText(RectTransform parent, string objectName, Vector2 anchor, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(parent.rect.width * size.x, parent.rect.height * size.y);
        rect.anchoredPosition = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = new Color(0.9f, 0.94f, 0.91f);
        text.alignment = alignment;
        text.textWrappingMode = objectName == "DiceResult" ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        return text;
    }

    private Button CreateHudButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject($"{label} Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(170f, 64f);
        buttonRect.anchoredPosition = position;
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = label == "ROLL" ? new Color(0.08f, 0.49f, 0.43f) : new Color(0.16f, 0.25f, 0.25f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonRect, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.font = TMP_Settings.defaultFontAsset;
        labelText.text = label;
        labelText.fontSize = 24;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        return button;
    }
}
