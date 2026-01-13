using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// A wrapper that provides safe dictionary access for RulesEngine expressions.
    /// Returns empty dictionary for missing keys instead of throwing KeyNotFoundException.
    /// </summary>
    public class SafeNestedDictionary
    {
        private readonly Dictionary<string, SafeInnerDictionary> _data = new Dictionary<string, SafeInnerDictionary>();

        /// <summary>
        /// Gets the value associated with the specified key, or an empty dictionary if the key doesn't exist.
        /// </summary>
        public SafeInnerDictionary this[string key]
        {
            get
            {
                if (_data.TryGetValue(key, out var value))
                {
                    return value;
                }
                // Return empty dictionary for missing keys
                return new SafeInnerDictionary();
            }
            set => _data[key] = value;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public int Count => _data.Count;
        public IEnumerable<string> Keys => _data.Keys;
    }

    /// <summary>
    /// Inner dictionary wrapper that returns null for missing keys instead of throwing KeyNotFoundException.
    /// </summary>
    public class SafeInnerDictionary
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        /// <summary>
        /// Gets the value associated with the specified key, or null if the key doesn't exist.
        /// </summary>
        public object this[string key]
        {
            get
            {
                if (_data.TryGetValue(key, out var value))
                {
                    return value;
                }
                // Return null for missing keys
                return null;
            }
            set => _data[key] = value;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public int Count => _data.Count;
        public IEnumerable<string> Keys => _data.Keys;
    }

    /// <summary>
    /// Safe nested dictionary for tasks: tasks[checklistId][taskId] returns TaskData
    /// </summary>
    public class SafeTasksDictionary
    {
        private readonly Dictionary<string, SafeTaskInnerDictionary> _data = new Dictionary<string, SafeTaskInnerDictionary>();

        public SafeTaskInnerDictionary this[string key]
        {
            get
            {
                if (_data.TryGetValue(key, out var value))
                {
                    return value;
                }
                return new SafeTaskInnerDictionary();
            }
            set => _data[key] = value;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public int Count => _data.Count;
        public IEnumerable<string> Keys => _data.Keys;
    }

    /// <summary>
    /// Inner dictionary for tasks: returns TaskData for each taskId
    /// </summary>
    public class SafeTaskInnerDictionary
    {
        private readonly Dictionary<string, TaskData> _data = new Dictionary<string, TaskData>();

        public TaskData this[string key]
        {
            get
            {
                if (_data.TryGetValue(key, out var value))
                {
                    return value;
                }
                // Return default TaskData for missing keys
                return new TaskData();
            }
            set => _data[key] = value;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);
        public int Count => _data.Count;
        public IEnumerable<string> Keys => _data.Keys;
    }

    /// <summary>
    /// Task data with strongly-typed properties for RulesEngine expressions
    /// </summary>
    public class TaskData
    {
        public bool isCompleted { get; set; } = false;
        public string name { get; set; } = string.Empty;
        public string completionNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Custom utility functions for RulesEngine expressions
    /// </summary>
    public static class RuleUtils
    {
        /// <summary>
        /// Returns current date (without time)
        /// </summary>
        public static DateTime Today() => DateTime.Today;

        /// <summary>
        /// Returns current date and time
        /// </summary>
        public static DateTime Now() => DateTime.Now;

        /// <summary>
        /// Calculate days between two dates
        /// </summary>
        public static int DaysBetween(DateTime start, DateTime end)
        {
            return (end.Date - start.Date).Days;
        }

        /// <summary>
        /// Calculate days between a date and today
        /// </summary>
        public static int DaysFromToday(DateTime date)
        {
            return (DateTime.Today - date.Date).Days;
        }

        /// <summary>
        /// Check if a date is a workday (not Saturday or Sunday)
        /// </summary>
        public static bool IsWorkday(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday 
                && date.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>
        /// Check if a string is null or empty
        /// </summary>
        public static bool IsEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Check if a string is not null or empty
        /// </summary>
        public static bool IsNotEmpty(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Check if a value is in a list
        /// </summary>
        public static bool InList(string? value, params string[] list)
        {
            if (string.IsNullOrEmpty(value) || list == null || list.Length == 0)
                return false;
            return list.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a value is in a comma-separated list string
        /// </summary>
        public static bool InList(string? value, string? commaSeparatedList)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(commaSeparatedList))
                return false;
            var list = commaSeparatedList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
            return list.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a value is not in a list
        /// </summary>
        public static bool NotInList(string? value, params string[] list)
        {
            return !InList(value, list);
        }

        /// <summary>
        /// Check if a value is not in a comma-separated list string
        /// </summary>
        public static bool NotInList(string? value, string? commaSeparatedList)
        {
            return !InList(value, commaSeparatedList);
        }

        /// <summary>
        /// Check if an object has a value (not null)
        /// </summary>
        public static bool HasValue(object? value)
        {
            return value != null;
        }

        /// <summary>
        /// Check if a string contains another string (case-insensitive)
        /// </summary>
        public static bool ContainsText(string? source, string? search)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(search))
                return false;
            return source.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a string starts with another string (case-insensitive)
        /// </summary>
        public static bool StartsWithText(string? source, string? prefix)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(prefix))
                return false;
            return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a string ends with another string (case-insensitive)
        /// </summary>
        public static bool EndsWithText(string? source, string? suffix)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(suffix))
                return false;
            return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the length of a string (returns 0 if null)
        /// </summary>
        public static int Length(string? value)
        {
            return value?.Length ?? 0;
        }

        /// <summary>
        /// Convert string to lowercase
        /// </summary>
        public static string ToLower(string? value)
        {
            return value?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Convert string to uppercase
        /// </summary>
        public static string ToUpper(string? value)
        {
            return value?.ToUpperInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Get absolute value of a number
        /// </summary>
        public static decimal Abs(decimal value)
        {
            return Math.Abs(value);
        }

        /// <summary>
        /// Round a number to specified decimal places
        /// </summary>
        public static decimal Round(decimal value, int decimals = 0)
        {
            return Math.Round(value, decimals);
        }
    }
}
