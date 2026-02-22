public enum TooltipLanguage
{
    English = 0,
    Russian = 1
}

public static class TooltipLocalization
{
    // Default project case: English tooltip localization.
    public static TooltipLanguage CurrentLanguage = TooltipLanguage.English;

    public static string Tr(string english, string russian)
    {
        return CurrentLanguage == TooltipLanguage.Russian ? russian : english;
    }
}

