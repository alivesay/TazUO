using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Managers;

public static class CommandHistoryManager
{
    private static readonly List<string> commandHistory = new();
    private static readonly char[] commandPrefixes = { '[', '.' };

    /// <summary>
    /// Adds a command to history if it starts with a command prefix
    /// </summary>
    /// <param name="input">The input text to check and potentially add</param>
    /// <returns>True if the command was added to history</returns>
    public static bool AddToHistoryIfCommand(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;

        // Check if input starts with any command prefix
        if (commandPrefixes.Any(prefix => input.StartsWith(prefix.ToString())))
        {
            // Only take the command name, not any arguments
            input = input.Split(' ')[0];

            // Don't add duplicates - remove existing and add to end for most recent
            if (commandHistory.Contains(input))
            {
                commandHistory.Remove(input);
            }

            commandHistory.Add(input);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets autocomplete suggestions for the given partial input
    /// </summary>
    /// <param name="partialInput">The partial command to complete</param>
    /// <returns>List of matching commands from history</returns>
    public static List<string> GetAutocompleteSuggestions(string partialInput)
    {
        if (string.IsNullOrEmpty(partialInput)) return new List<string>();

        // Get commands that start with the partial input (case-insensitive)
        var matches = commandHistory
            .Where(cmd => cmd.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(cmd => cmd)
            .ToList();

        return matches;
    }

    /// <summary>
    /// Gets the best autocomplete match for the given partial input
    /// </summary>
    /// <param name="partialInput">The partial command to complete</param>
    /// <returns>The best matching command or null if no matches</returns>
    public static string GetBestAutocompletion(string partialInput)
    {
        var suggestions = GetAutocompleteSuggestions(partialInput);
        return suggestions.FirstOrDefault();
    }

    /// <summary>
    /// Gets all commands in history (most recent first)
    /// </summary>
    /// <returns>List of all commands in reverse chronological order</returns>
    public static List<string> GetAllCommands()
    {
        var result = new List<string>(commandHistory);
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Clears the command history
    /// </summary>
    public static void ClearHistory()
    {
        commandHistory.Clear();
    }
}
