namespace EdeskyScraper;

internal class Colors
{
    public static int GetTimeBasedColor(DateTime datetime)
    {
        try
        {
            int colorPos = (int)datetime.DayOfWeek % Array.Length;
            int shadowPos = datetime.Hour % Array[colorPos].Length;
            string colorHex = Array[colorPos][shadowPos];
            int parsed = int.Parse(colorHex.AsSpan(1), System.Globalization.NumberStyles.HexNumber);

            return parsed switch
            {
                < 0 => 0, //black
                > 16777215 => 16777215, //white
                _ => parsed //from table
            };
        }
        catch
        {
            return 1127128; //blue
        }
    }

    public static string[][] Array { get; } =
    [
        [ "#fadbd8", "#f5b7b1", "#f1948a", "#ec7063", "#e74c3c", "#cb4335", "#b03a2e", "#943126", "#78281f", ],
        [ "#e8daef", "#d2b4de", "#bb8fce", "#a569bd", "#8e44ad", "#7d3c98", "#6c3483", "#5b2c6f", "#4a235a", ],
        [ "#d6eaf8", "#aed6f1", "#85c1e9", "#5dade2", "#3498db", "#2e86c1", "#2874a6", "#21618c", "#1b4f72", ],
        [ "#d0ece7", "#a2d9ce", "#73c6b6", "#45b39d", "#16a085", "#138d75", "#117a65", "#0e6655", "#0b5345", ],
        [ "#d5f5e3", "#abebc6", "#82e0aa", "#58d68d", "#2ecc71", "#28b463", "#239b56", "#1d8348", "#186a3b", ],
        [ "#fdebd0", "#fad7a0", "#f8c471", "#f5b041", "#f39c12", "#d68910", "#b9770e", "#9c640c", "#7e5109", ],
        [ "#f6ddcc", "#edbb99", "#e59866", "#dc7633", "#d35400", "#ba4a00", "#a04000", "#873600", "#6e2c00", ],
        [ "#f2f3f4", "#e5e7e9", "#d7dbdd", "#cacfd2", "#bdc3c7", "#a6acaf", "#909497", "#797d7f", "#626567", ],
        [ "#e5e8e8", "#ccd1d1", "#b2babb", "#99a3a4", "#7f8c8d", "#707b7c", "#616a6b", "#515a5a", "#424949", ],
        [ "#d5d8dc", "#abb2b9", "#808b96", "#566573", "#2c3e50", "#273746", "#212f3d", "#1c2833", "#17202a", ],
    ];
}