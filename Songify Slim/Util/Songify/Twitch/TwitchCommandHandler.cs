using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Songify_Slim.Models;
using TwitchLib.Client.Models;

namespace Songify_Slim.Util.Songify.Twitch
{
    public delegate Task CommandHandlerDelegate(ChatMessage message, TwitchCommand command, TwitchCommandParams cmdParams);

    public static class TwitchCommandHandler
    {
        // Mapping by command Name (unchanged).
        private static readonly Dictionary<string, TwitchCommand> CommandsByName =
            new Dictionary<string, TwitchCommand>(StringComparer.OrdinalIgnoreCase);

        // Mapping by trigger or alias → TwitchCommand.
        // We still call it CommandsByTrigger to keep your public surface area unchanged.
        private static readonly Dictionary<string, TwitchCommand> CommandsByTrigger =
            new Dictionary<string, TwitchCommand>(StringComparer.OrdinalIgnoreCase);

        // Command handlers keyed by the canonical trigger (command.Trigger).
        private static readonly Dictionary<string, CommandHandlerDelegate> CommandHandlers =
            new Dictionary<string, CommandHandlerDelegate>(StringComparer.OrdinalIgnoreCase);

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            if (s.StartsWith("!")) s = s.Substring(1);
            return s;
        }

        /// <summary>
        /// Registers a command using its Name as the primary key,
        /// and indexes it by its trigger and all aliases for execution.
        /// The handler is registered under the command's canonical Trigger.
        /// </summary>
        public static void RegisterCommand(TwitchCommand command, CommandHandlerDelegate handler)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // --- Register by Name ---
            if (!CommandsByName.ContainsKey(command.Name))
                CommandsByName.Add(command.Name, command);
            else
                CommandsByName[command.Name] = command;

            // --- Register by canonical trigger ---
            string canonical = Normalize(command.Trigger);
            if (!string.IsNullOrEmpty(canonical))
            {
                CommandsByTrigger[canonical] = command;
            }

            // --- Register aliases (all map to the same TwitchCommand) ---
            if (command.Aliases != null)
            {
                foreach (string alias in command.Aliases)
                {
                    string key = Normalize(alias);
                    if (string.IsNullOrEmpty(key)) continue;
                    CommandsByTrigger[key] = command;
                }
            }

            // --- Register handler by canonical trigger only ---
            if (!string.IsNullOrEmpty(canonical))
                CommandHandlers[canonical] = handler;
        }

        /// <summary>
        /// Executes a command based on the trigger or alias parsed from the chat message.
        /// </summary>
        public static bool TryExecuteCommand(ChatMessage message, TwitchCommandParams cmdParams)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Message))
                return false;

            // Extract first token (e.g., "!sr params..." -> "sr")
            string firstToken = message.Message.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries)[0];
            string key = Normalize(firstToken);
            if (string.IsNullOrEmpty(key)) return false;

            // Look up command by trigger OR alias
            if (!CommandsByTrigger.TryGetValue(key, out TwitchCommand command) || command is not { IsEnabled: true })
                return false;

            // Always resolve handler using the command's canonical trigger
            string canonical = Normalize(command.Trigger);
            if (string.IsNullOrEmpty(canonical)) return false;

            if (!CommandHandlers.TryGetValue(canonical, out CommandHandlerDelegate handler) || handler == null)
                return false;

            handler(message, command, cmdParams);
            return true;
        }

        /// <summary>
        /// Retrieves a command by its Name (must also be enabled).
        /// </summary>
        public static bool TryGetCommand(string name, out TwitchCommand command)
        {
            command = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (CommandsByName.TryGetValue(name, out TwitchCommand found) && found?.IsEnabled == true)
            {
                command = found;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public static void ClearCommands()
        {
            CommandsByName.Clear();
            CommandsByTrigger.Clear();
            CommandHandlers.Clear();
        }
    }
}
