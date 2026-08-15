
namespace TileIconifier.Core.Shortcut
{
    [Serializable]
    public class ShortcutItemTarget
    {
        public required string FilePath { get; set; }
        public string? Arguments { get; set; }
        public string? IconLocation { get; set; }
    }
}
