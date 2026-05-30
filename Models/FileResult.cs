// ============================================================
// File: FileResult.cs
// Project: OpticCli
// Namespace: OpticCli.Models
// Description: Defines models for file search results and command
//              history entries displayed in the UI.
// ============================================================
namespace OpticCli.Models
{
    // Represents a single file shown in the file search results
    public class FileResult
    {
        public string Icon { get; init; } = "📄";
        public string Name { get; init; } = "";
        public string Size { get; init; } = "";
        public string Type { get; init; } = "";
        public string Modified { get; init; } = "";
        public string Path { get; init; } = "";
    }

    // Represents one past command run saved in the history list
    public class HistoryEntry
    {
        public string Time { get; set; } = "";
        public string Query { get; set; } = "";
        public string Command { get; set; } = "";
        public string Risk { get; set; } = "";
        public string Status { get; set; } = "";
        public string Duration { get; set; } = "";
        public string ResultSummary { get; set; } = "";
        public string DateGroup { get; set; } = "";
    }
}