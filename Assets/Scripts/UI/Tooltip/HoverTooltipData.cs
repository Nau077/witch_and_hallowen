public struct HoverTooltipData
{
    public string title;
    public string levelLine;
    public string priceLine;
    public string description;

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(title)
            && string.IsNullOrEmpty(levelLine)
            && string.IsNullOrEmpty(priceLine)
            && string.IsNullOrEmpty(description);
    }
}
