using System.Globalization;

public class MesLancamentoDTO
{
    public int Ano { get; set; }
    public int Mes { get; set; }

    public string Display
    {
        get
        {
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Mes);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName) + $"/{Ano % 100:D2}";
        }
    }

    public override string ToString() => Display;
}
