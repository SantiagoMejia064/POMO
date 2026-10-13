namespace Pomo.Shared.Enums
{
    public enum StorePurchaseResult
    {
        Success = 0,
        InvalidItem = 1,
        PriceNotConfigured = 2,
        AlreadyOwned = 3,
        InsufficientCups = 4,
        InventoryUpdateFailed = 5
    }
}
