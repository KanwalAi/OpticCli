// ============================================================
// File: AIService.cs
// Project: OpticCli
// Namespace: OpticCli.Services
// Description: Handles all communication with the Groq LLM API.
//              Provides AI-generated PowerShell command suggestions
//              and error explanations. Includes rate-limiting and
//              lockout protection against repeated API failures.
// ============================================================

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpticCli.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

// This class talks to the Groq AI API and gets PowerShell command suggestions
namespace OpticCli.Services
{
    public class AIService
    {
        // API settings - key, url, and which AI model to use
        private const string ApiKey = "";
        private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";
        private const string Model = "llama-3.3-70b-versatile";

        // This tells the AI how to behave - always return 3 commands in JSON, PowerShell only, no markdown
        private const string SystemPrompt =
            "You are an expert Windows PowerShell and CMD assistant inside OpticCli. " +
            "When given a natural-language query, respond with ONLY a valid JSON array. " +
            "No markdown, no code fences, no explanation — just raw JSON. " +
            "Each element must have exactly these fields: " +
            "\"code\": the complete ready-to-run PowerShell or CMD command, " +
            "\"description\": 2-3 sentences explaining what it does for a non-technical user, " +
            "\"risk\": exactly one of: \"safe\", \"medium\", or \"high\". " +
            "Risk rules: safe=read-only only lists data, medium=modifies non-critical state, high=destructive or requires admin. " +
            "ALWAYS return exactly 3 suggestions. Order from safest to most powerful. " +
            "IMPORTANT: Always use PowerShell syntax only. Never use CMD syntax like 'type nul', 'echo.', 'copy con'. " +
            "For creating files use: New-Item -ItemType File -Name 'filename.txt'. " +
            "For creating folders use: New-Item -ItemType Directory -Name 'foldername'. ";

        // ── Lockout tracking ──────────────────────────────────────────────────
        // Track how many times the API failed so we can lock the user out after 3 tries
        private static int _failedAttempts = 0;
        private static DateTime _lockoutUntil = DateTime.MinValue;
        private const int MaxAttempts = 3;
        private const int LockoutMinutes = 15;

        // One shared HTTP client for all requests (better than creating a new one each time)
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        static AIService()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        public async Task<List<CommandSuggestion>> GetSuggestionsAsync(string userQuery)
        {
            // ── Check lockout ───────────────────────────────────────────
            // If the user failed too many times, block them until the lockout time is over
            if (DateTime.Now < _lockoutUntil)
            {
                var mins = (int)(_lockoutUntil - DateTime.Now).TotalMinutes + 1;
                throw new Exception(
                    $"🔒 Access locked for {mins} more minute(s) due to repeated failures.\n" +
                    $"Please wait before trying again.");
            }

            // Build request
            // Build the request we'll send to the AI - includes our instructions and the user's question
            var requestJson = new JObject
            {
                ["model"] = Model,
                ["temperature"] = 0.2,
                ["max_tokens"] = 800,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = SystemPrompt },
                    new JObject { ["role"] = "user",   ["content"] = userQuery    }
                }
            };

            var content = new StringContent(
                requestJson.ToString(Formatting.None),
                Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            string responseJson;

            // Send the request - if anything goes wrong (no internet, timeout) count it as a failed attempt
            try
            {
                response = await _http.PostAsync(Endpoint, content);
                responseJson = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // Network/timeout failure counts as a failed attempt
                HandleFailedAttempt();
                throw new Exception(GetAttemptMessage(ex.Message));
            }

            // ── Handle API errors ─────────────────────────────────────────────
            if (!response.IsSuccessStatusCode)
            {
                HandleFailedAttempt();
                throw new Exception(GetAttemptMessage(
                    $"Groq API error {(int)response.StatusCode}: {responseJson}"));
            }

            // ── Success — reset counter ───────────────────────────────────────
            // It worked, so reset the fail counter
            _failedAttempts = 0;

            var jObj = JObject.Parse(responseJson);
            var message = jObj["choices"][0]["message"]["content"].ToString();
            return ParseSuggestions(message);
        }

        // Add 1 to the fail count, and lock the user out if they hit 3 failures
        private static void HandleFailedAttempt()
        {
            _failedAttempts++;
            if (_failedAttempts >= MaxAttempts)
            {
                _lockoutUntil = DateTime.Now.AddMinutes(LockoutMinutes);
                _failedAttempts = 0;
            }
        }

        private static string GetAttemptMessage(string baseError)
        {
            // Already at max — lockout just triggered
            if (DateTime.Now < _lockoutUntil)
            {
                return $"🔒 Access locked for {LockoutMinutes} minutes due to repeated failures.\n" +
                       $"Please wait before trying again.";
            }

            int remaining = MaxAttempts - _failedAttempts;

            return _failedAttempts switch
            {
                1 => $"⚠ Invalid API key or connection error. Please check your settings.\n" +
                     $"({remaining} attempt(s) remaining before lockout)",
                2 => $"⚠ API call failed again. {remaining} attempt(s) remaining before 15-minute lockout.\n" +
                     $"Error: {baseError}",
                _ => baseError
            };
        }

        // Turn the AI's raw text response into a proper list of suggestions
        // Also handles cases where the AI wraps it in markdown code blocks even though we told it not to
        private static List<CommandSuggestion> ParseSuggestions(string jsonText)
        {
            var result = new List<CommandSuggestion>();
            try
            {
                jsonText = jsonText.Trim();

                if (jsonText.StartsWith("```"))
                {
                    int start = jsonText.IndexOf('[');
                    int end = jsonText.LastIndexOf(']');
                    if (start >= 0 && end >= 0)
                        jsonText = jsonText.Substring(start, end - start + 1);
                }

                var array = JArray.Parse(jsonText);
                foreach (var item in array)
                {
                    result.Add(new CommandSuggestion
                    {
                        Code = item["code"]?.ToString() ?? "",
                        Description = item["description"]?.ToString() ?? "",
                        Risk = ParseRisk(item["risk"]?.ToString() ?? "safe")
                    });
                }
            }
            catch (Exception ex)
            {
                result.Add(new CommandSuggestion
                {
                    Code = "Get-Process | Sort-Object CPU -Descending",
                    Description = $"Could not parse AI response: {ex.Message}",
                    Risk = RiskLevel.Safe
                });
            }
            return result;
        }

        private static RiskLevel ParseRisk(string risk)
        {
            switch (risk.ToLower().Trim())
            {
                case "medium": return RiskLevel.Medium;
                case "high": return RiskLevel.High;
                default: return RiskLevel.Safe;
            }
        }

        // Ask the AI why a command failed and how to fix it
        public async Task<string> ExplainErrorAsync(string command, string errorText)
        {
            var requestJson = new JObject
            {
                ["model"] = Model,
                ["temperature"] = 0.3,
                ["max_tokens"] = 400,
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"]    = "system",
                        ["content"] = "You are a helpful Windows PowerShell assistant. " +
                                      "When given a failed command and its error message, respond in exactly 3 lines:\n" +
                                      "Line 1: WHY: (one sentence explaining why it failed, for a non-technical user)\n" +
                                      "Line 2: FIX: (one concrete suggestion to fix it)\n" +
                                      "Line 3: TIP: (one short pro tip related to this command)\n" +
                                      "No markdown, no extra text."
                    },
                    new JObject
                    {
                        ["role"]    = "user",
                        ["content"] = $"Command: {command}\nError: {errorText}"
                    }
                }
            };

            var content = new StringContent(
                requestJson.ToString(Formatting.None),
                Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(Endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return "Could not get AI explanation.";

            var jObj = JObject.Parse(responseJson);
            return jObj["choices"][0]["message"]["content"].ToString().Trim();
        }
    }
}