using System;
using System.Collections.Generic;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace UsnParser
{
    internal static class ConsoleExtensions
    {
        public static void Write(this IConsole console, ConsoleColor color, string message)
        {
            console.ForegroundColor = color;
            console.Write(message);
            console.ResetColor();
        }

        public static void WriteLine(this IConsole console, ConsoleColor color, string message)
        {
            console.ForegroundColor = color;
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void PrintError(this IConsole console, string message)
        {
            console.WriteLine(ConsoleColor.Red, message);
        }
    }
}
