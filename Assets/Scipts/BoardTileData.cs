using UnityEngine;

public enum BoardTileType
{
    Go,
    Property,
    Railroad,
    Utility,
    Tax,
    Chance,
    Jail,
    FreeParking,
    GoToJail
}

public enum PropertyOwner
{
    None,
    Player,
    Bot
}

public class BoardTileData : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int[] ChanceSpaces = { 2, 7, 17, 22, 33, 36 };
    private static readonly int[] RailroadSpaces = { 5, 15, 25, 35 };
    private static readonly int[] UtilitySpaces = { 12, 28 };

    private Renderer[] tileRenderers;
    private MaterialPropertyBlock propertyBlock;

    public int Index { get; private set; }
    public string DisplayName { get; private set; }
    public BoardTileType Type { get; private set; }
    public PropertyOwner Owner { get; private set; }
    public int Price { get; private set; }
    public int Rent { get; private set; }

    private void Awake()
    {
        tileRenderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Configure(int tileIndex)
    {
        Index = tileIndex;
        DisplayName = GetTileName(tileIndex);
        Type = GetTileType(tileIndex);
        Price = GetPrice(tileIndex, Type);
        Rent = Mathf.Max(10, Price / 5);
        Owner = PropertyOwner.None;
    }

    public bool IsPurchasable => Type == BoardTileType.Property || Type == BoardTileType.Railroad || Type == BoardTileType.Utility;

    public void SetOwner(PropertyOwner owner)
    {
        Owner = owner;
    }

    public void SetHighlight(Color color)
    {
        if (tileRenderers == null) { tileRenderers = GetComponentsInChildren<Renderer>(); }
        if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }

        foreach (Renderer tileRenderer in tileRenderers)
        {
            tileRenderer.GetPropertyBlock(propertyBlock);
            if (tileRenderer.sharedMaterial != null && tileRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else
            {
                propertyBlock.SetColor(ColorId, color);
            }
            tileRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void ClearHighlight()
    {
        if (tileRenderers == null) { tileRenderers = GetComponentsInChildren<Renderer>(); }
        foreach (Renderer tileRenderer in tileRenderers)
        {
            tileRenderer.SetPropertyBlock(null);
        }
    }

    private static BoardTileType GetTileType(int index)
    {
        if (index == 0) { return BoardTileType.Go; }
        if (index == 10) { return BoardTileType.Jail; }
        if (index == 20) { return BoardTileType.FreeParking; }
        if (index == 30) { return BoardTileType.GoToJail; }
        if (index == 4 || index == 38) { return BoardTileType.Tax; }
        if (System.Array.IndexOf(ChanceSpaces, index) >= 0) { return BoardTileType.Chance; }
        if (System.Array.IndexOf(RailroadSpaces, index) >= 0) { return BoardTileType.Railroad; }
        if (System.Array.IndexOf(UtilitySpaces, index) >= 0) { return BoardTileType.Utility; }
        return BoardTileType.Property;
    }

    private static int GetPrice(int index, BoardTileType type)
    {
        if (type == BoardTileType.Railroad) { return 200; }
        if (type == BoardTileType.Utility) { return 150; }
        if (type != BoardTileType.Property) { return 0; }
        return 60 + (index % 10) * 20;
    }

    private static string GetTileName(int index)
    {
        if (index == 0) { return "GO"; }
        if (index == 10) { return "Jail / Visiting"; }
        if (index == 20) { return "Free Parking"; }
        if (index == 30) { return "Go To Jail"; }
        if (index == 4) { return "Income Tax"; }
        if (index == 38) { return "Luxury Tax"; }
        if (GetTileType(index) == BoardTileType.Chance) { return "Chance"; }
        if (GetTileType(index) == BoardTileType.Railroad) { return "Railroad"; }
        if (GetTileType(index) == BoardTileType.Utility) { return "Utility"; }
        return $"Property {index + 1}";
    }
}