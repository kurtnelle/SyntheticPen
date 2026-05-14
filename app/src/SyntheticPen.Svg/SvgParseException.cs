namespace SyntheticPen.Svg;

public sealed class SvgParseException : Exception
{
    public SvgParseException(string message, int? lineNumber = null) : base(message)
    {
        LineNumber = lineNumber;
    }

    public SvgParseException(string message, Exception inner, int? lineNumber = null) : base(message, inner)
    {
        LineNumber = lineNumber;
    }

    public int? LineNumber { get; }
}
