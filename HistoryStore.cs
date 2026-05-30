// ============================================================
// File: HistoryStore.cs
// Project: OpticCli
// Namespace: OpticCli
// Description: Manages saving and loading command history to a
//              local JSON file. Keeps a max of 100 entries using
//              a FIFO cap (newest first, oldest removed).
// ============================================================

using Newtonsoft.Json;
using OpticCli.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpticCli
{
    public static class HistoryStore
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpticCli",
            "history.json");

        public static List<HistoryEntry> Entries { get; private set; }
            = new List<HistoryEntry>();

        // Load existing history from disk when the app starts
        static HistoryStore()
        {
            Load();
        }

        public static void Add(HistoryEntry entry)
        {
            Entries.Insert(0, entry);

            // FIFO cap: remove oldest entry if over limit
            if (Entries.Count > 100)
                Entries.RemoveAt(Entries.Count - 1); // oldest is at the end

            Save();
        }

        public static void Clear()
        {
            Entries.Clear();
            Save();
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    Entries = JsonConvert.DeserializeObject<List<HistoryEntry>>(json)
                              ?? new List<HistoryEntry>();
                }
            }
            catch
            {
                Entries = new List<HistoryEntry>();
            }
        }

        // Save to AppData so history persists across sessions
        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var json = JsonConvert.SerializeObject(Entries, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}