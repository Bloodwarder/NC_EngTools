namespace LoaderCore.Utilities
{
    public record ErrorEntry
    {
        public ErrorEntry(string elementName, string message)
        {
            ElementName = elementName;
            Message = message;
        }
        public string ElementName { get; }
        public string Message { get; }
    }
}
