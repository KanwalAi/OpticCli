// ============================================================
// File: CommandSuggestions.cs
// Project: OpticCli
// Namespace: OpticCli.Models
// Description: Defines the core data models used across the app.
//              Contains the RiskLevel enum and CommandSuggestion
//              class that holds each AI-generated command result.
// ============================================================
namespace OpticCli.Models
{
    // The 3 possible risk levels for a command
    public enum RiskLevel { Safe, Medium, High }

    // Holds one AI-suggested command with its description, risk, and label
    public class CommandSuggestion
    {
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public RiskLevel Risk { get; set; } = RiskLevel.Safe;
        public bool IsSelected { get; set; } = false;

        // Shows a short text label with a symbol based on the risk level
        public string RiskLabel => Risk switch
        {
            RiskLevel.Safe => "● SAFE",
            RiskLevel.Medium => "▲ MEDIUM",
            RiskLevel.High => "■ HIGH RISK",
            _ => "UNKNOWN"
        };
    }

}