namespace Core.Dto;

public sealed record ImportStats(int Accepted, int Skipped)
{
    public int Total => Accepted + Skipped;

    public double ErrorPercent => Total == 0 ? 0 : (double)Skipped / Total * 100;

    public override string ToString() =>
        $"Усього: {Total} | Прийнято: {Accepted} | Пропущено: {Skipped} | Помилок: {ErrorPercent:F1}%";
}