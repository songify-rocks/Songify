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
        // Mapping by command Name.
        private static readonly Dictionary<string, TwitchCommand> CommandsByName =
            new Dictionary<string, TwitchCommand>(StringComparer.OrdinalIgnoreCase);

        // Mapping by command Trigger.
        private static readonly Dictionary<string, TwitchCommand> CommandsByTrigger =
            new Dictionary<string, TwitchCommand>(StringComparer.OrdinalIgnoreCase);

        // Command handlers keyed by trigger.
        private static readonly Dictionary<string, CommandHandlerDelegate> CommandHandlers =
            new Dictionary<string, CommandHandlerDelegate>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a command using its Name as the primary key,
        /// and also indexes it by its trigger for execution.
        /// </summary>
        public static void RegisterCommand(TwitchCommand command, CommandHandlerDelegate handler)
        {
            // Register by name.
            if (!CommandsByName.ContainsKey(command.Name))
            {
                CommandsByName.Add(command.Name, command);
            }
            else
            {
                // Optionally update the command if duplicates are not allowed.
                CommandsByName[command.Name] = command;
            }

            // Also register the command keyed by trigger.
            if (!CommandsByTrigger.ContainsKey(command.Trigger))
            {
                CommandsByTrigger.Add(command.Trigger, command);
            }
            else
            {
                CommandsByTrigger[command.Trigger] = command;
            }

            // Register the command handler keyed by trigger.
            if (!CommandHandlers.ContainsKey(command.Trigger))
            {
                CommandHandlers.Add(command.Trigger, handler);
            }
            else
            {
                CommandHandlers[command.Trigger] = handler;
            }
        }

        /// <summary>
        /// Executes a command based on the trigger parsed from the chat message.
        /// </summary>
        public static bool TryExecuteCommand(ChatMessage message, TwitchCommandParams cmdParams)
        {
            // Extract the trigger from the message.
            // For example, assume message.Message is something like "!sr some parameters"
            string trigger = message.Message.Split(' ')[0];

            if (trigger.StartsWith("!"))
                trigger = trigger.Substring(1);

            if (!CommandsByTrigger.TryGetValue(trigger, out TwitchCommand command) || !command.IsEnabled) return false;
            if (!CommandHandlers.TryGetValue(trigger, out CommandHandlerDelegate handler)) return false;
            handler(message, command, cmdParams);
            return true;
        }

        /// <summary>
        /// Retrieves a command by its name.
        /// </summary>
        public static bool TryGetCommand(string name, out TwitchCommand command)
        {
            command = null;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Lookup by command Name.
            if (CommandsByName.TryGetValue(name, out command))
            {
                if (command.IsEnabled)
                    return true;
            }

            command = null;
            return false;
        }

        // This method clears all the registrations.
        public static void ClearCommands()
        {
            CommandsByName.Clear();
            CommandsByTrigger.Clear();
            CommandHandlers.Clear();
        }
    }
}