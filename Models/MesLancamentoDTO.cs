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
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(monthName);
            }
            else
                return Ano.ToString();
        }
    }

    public override string ToString() => Display;
}
