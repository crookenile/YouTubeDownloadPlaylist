class FormatInfo
{
    public string FormatCode { get; set; } = "";
    public string Label { get; set; } = "";
    public bool IsVideoOnly { get; set; }
    public (int height, int Item2, int fps) SortKey { get; set; }
}
