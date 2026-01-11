using System.Globalization;

public class MesAnoLancamentoDTO
{
    public int Ano { get; set; }
    public int Mes { get; set; }

    public string Display
    {
        get
        {
            if(Mes != 0)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Mes);
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName) + $"/{Ano % 100:D2}";
            }
            else
                return Ano.ToString();
        }
    }

    public override string ToString() => Display;
}
