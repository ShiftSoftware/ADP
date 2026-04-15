namespace ShiftSoftware.ADP.Menus.Data.DataServices;

public interface IMenuPartPriceService
{
    ValueTask<MenuPartPrice?> StockByPartNumberAsync(string partNumber);
    ValueTask<IEnumerable<MenuPartPrice>> StockByPartNumbersAsync(IEnumerable<string> partNumbers);
}
