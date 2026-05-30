# OpticCli — AI-Assisted Visual Command Discovery and Execution System

An intelligent Windows desktop application that translates natural language
into PowerShell commands, with risk analysis, visual output, and command history.

---
--------------------------------------------------------------------
## Quick Run By mysetup.exe (No Installation Required)

If you just want to run the app immediately:
1. Extract `mysetup.zip`
2. Double-click `mysetup.exe`
3. App launches instantly — no setup needed
4. Requires Windows 10/11 and internet connection for AI features

---
---------------------------------------------------------------------

## Run By Visual Studio roject

## Prerequisites

Before running OpticCli, ensure you have:

- Windows 10 or Windows 11 (64-bit)
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework) (pre-installed on Windows 10/11)
- [PowerShell 7.4+](https://github.com/PowerShell/PowerShell/releases)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community or higher)
- A valid Groq API key — get one free at https://console.groq.com

---

## Installation & Setup

1. **Clone or extract** the project folder
2. Open `OpticCli.sln` in Visual Studio 2022
3. Restore NuGet packages:
   `Tools → NuGet Package Manager → Restore Packages`
4. **Set your API key** — open `Services/AIService.cs` and replace:
   
	`private const string ApiKey = "your_groq_api_key_here";`

5. Build the solution: `Ctrl + Shift + B`
6. Run the application: `Ctrl + F5`

---

## NuGet Dependencies

| Package | Version | Purpose |
|---|---|---|
| Newtonsoft.Json | 13.x | JSON parsing for AI responses and history |
| System.Speech | 8.x | Windows Speech Recognition (voice input) |

---

## Project Structure

```
OpticCli/
├── Models/
│   ├── CommandSuggestion.cs      # AI suggestion model + RiskLevel enum
│   └── HistoryEntry.cs           # History and file result models
├── Services/
│   └── AIService.cs              # Groq API integration (llama-3.3-70b-versatile)
├── Views/
│   ├── HomeView.xaml/.cs         # Natural language search screen
│   ├── SuggestionsView.xaml/.cs  # AI command suggestions + risk badges
│   ├── CustomizeView.xaml/.cs    # Parameter customization GUI
│   ├── OutputView.xaml/.cs       # Execution results + table visualization
│   ├── HistoryView.xaml/.cs      # Command history log
│   └── TitleBarControl.xaml/.cs  # Custom title bar
├── Converters/
│   └── Converters.cs             # Risk-to-color WPF value converters
├── ShellWrapper.cs               # PowerShell/CMD execution engine
├── HistoryStore.cs               # JSON-based persistent history (100-entry FIFO)
├── App.xaml/.cs                  # Application entry point + global styles
└── IsExternalInit.cs
└── README.md
```

---

## Features

| # | Feature | Description |
|---|---|---|
| 1 | Natural Language Search | Type plain English, get PowerShell commands |
| 2 | Risk Analysis | Every command rated Safe / Medium / High |
| 3 | Output Visualization | Results parsed into interactive tables or in raw form |
| 4 | Parameter GUI | Customize flags and paths before execution |
| 5 | Command History | Searchable log with re-run capability |
| 6 | Error Explanation | AI explains failed commands in plain language |
| 7 | Data Export | Export results to CSV or JSON if valid |

---

## How to Use

1. Type a natural language request in the search box
   e.g. `"Find all PDF files on this drive"`
2. Click **Discover Commands** or press `Enter`
3. Review the AI-suggested commands with their risk badges
4. Click **Customize Selected** to adjust parameters, or **Run As-Is**
5. Review the structured output table if valid or in raw form
6. Export results or view command history as needed

---

## Safety Notes

- **High Risk commands** require explicit confirmation before execution
- Commands are **never auto-executed** — user approval is always required
- The AI service enforces a **3-attempt lockout** (15 minutes) on API failures
- History is stored locally at `%AppData%\OpticCli\history.json`

---

## Team

| Name | ID | Role |
|---|---|---|
| Kanwal Fatima | 24I-3128 | Frontend, AI Integration, Command Parsing |
| Nishan Muhammad Nusky | 24I-0124 | Architecture, Database, Documentation |

**Department of Computer Science**
**National University of Computer and Emerging Sciences, Islamabad**
