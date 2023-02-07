using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Views;
using Utils.General;
using VRage.Game.ModAPI;
using VRageMath;

namespace Utils.Torch
{
    internal static class CommandModuleUtils
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static void EnsureInvokedByPlayer(this CommandModule self)
        {
            self.Context.Player.ThrowIfNull("Must be called by a player");
        }

        public static void CatchAndReport(this CommandModule self, Action f)
        {
            try
            {
                f();
            }
            catch (Exception e)
            {
                var id = ReportGenerator.Log(self, e);
                self.Context.Respond($"Something broke: \"{e.Message}\"; {id}", Color.Red);
            }
        }

        public static async void CatchAndReportAsync(this CommandModule self, Func<Task> f)
        {
            try
            {
                await f();
            }
            catch (Exception e)
            {
                var id = ReportGenerator.Log(self, e);
                self.Context.Respond($"Something broke: \"{e.Message}\"; {id}", Color.Red);
            }
        }

        public static void ShowCommands(this CommandModule self)
        {
            var level = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var commands = GetCommandMethods(self.GetType())
                .Where(p => p.Permission <= level)
                .ToArray();

            if (!commands.Any())
            {
                self.Context.Respond("No accessible commands found");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Commands:");

            var commandManager = self.Context.Torch.CurrentSession.Managers.GetManager<CommandManager>();
            var foundAnyCommands = false;
            foreach (var node in commandManager.Commands.WalkTree())
            {
                var command = node.Command;
                if (command == null) continue;

                if (command.Module != self.GetType()) continue;

                // check access level
                if (self.Context.Player is { } player &&
                    player.PromoteLevel < command.MinimumPromoteLevel)
                {
                    continue;
                }

                var syntaxHelp = command.SyntaxHelp;
                var description = command.Description.OrNull() ?? "no description";
                msgBuilder.AppendLine($"{syntaxHelp} -- {description}");
                foundAnyCommands = true;
            }

            if (!foundAnyCommands)
            {
                self.Context.Respond("No accessible commands found");
                return;
            }

            self.Context.Respond(msgBuilder.ToString());
        }

        public static IEnumerable<(CommandAttribute Command, MyPromoteLevel Permission)> GetCommandMethods(Type type)
        {
            foreach (var method in type.GetMethods())
            {
                if (!method.TryGetAttribute<CommandAttribute>(out var command)) continue;
                if (!method.TryGetAttribute<PermissionAttribute>(out var permission)) continue;
                yield return (command, permission.PromoteLevel);
            }
        }

        public static void GetOrSetProperty(this CommandModule self, object config)
        {
            if (!self.Context.Args.TryGetFirst(out var propertyNameOrIndex))
            {
                self.ShowConfigurableProperties(config);
                return;
            }

            if (propertyNameOrIndex == "all")
            {
                self.ShowConfigurablePropertyValues(config);
                return;
            }

            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();

            var propertyName = propertyNameOrIndex;
            if (int.TryParse(propertyNameOrIndex, out var propertyIndex))
            {
                var maxPropertyIndex = properties.Length - 1;
                if (maxPropertyIndex < propertyIndex)
                {
                    self.Context.Respond($"Index out of bounds; max: {maxPropertyIndex}", Color.Red);
                    self.ShowConfigurableProperties(config);
                    return;
                }

                propertyName = properties[propertyIndex].Name;
            }

            if (!properties.TryGetFirst(p => p.Name == propertyName, out var property))
            {
                self.Context.Respond($"Property not found: \"{propertyName}\"", Color.Red);
                self.ShowConfigurableProperties(config);
                return;
            }

            if (property.TryGetAttribute(out ConfigPropertyAttribute prop) &&
                !prop.IsVisibleTo(promoLevel))
            {
                self.Context.Respond($"Property not visible: \"{propertyName}\"", Color.Red);
                self.ShowConfigurableProperties(config);
                return;
            }

            Log.Info(self.Context.Args.ToStringSeq());

            if (self.Context.Args.TryGetElementAt(1, out var arg))
            {
                if (promoLevel < MyPromoteLevel.Moderator)
                {
                    throw new InvalidOperationException("not moderator");
                }

                var newValue = ParsePrimitive(property.PropertyType, arg);
                property.SetValue(config, newValue);
                Log.Info($"set value via config command: {config} {newValue}");
            }

            var value = property.GetValue(config);
            self.Context.Respond($"> {propertyName}: {value}");
        }

        static void ShowConfigurablePropertyValues(this CommandModule self, object config)
        {
            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();
            if (!properties.Any())
            {
                self.Context.Respond("No configurable properties");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Config properties:");
            foreach (var property in properties)
            {
                var name = property.Name;
                var value = property.GetValue(config);
                msgBuilder.AppendLine($"> {name}: {value}");
            }

            msgBuilder.AppendLine("To update, either `config <index> <value>` or `config <name> <value>`.");

            self.Context.Respond(msgBuilder.ToString());
        }

        static void ShowConfigurableProperties(this CommandModule self, object config)
        {
            var promoLevel = self.Context.Player?.PromoteLevel ?? MyPromoteLevel.Admin;
            var properties = GetConfigurableProperties(config, promoLevel).ToArray();
            if (!properties.Any())
            {
                self.Context.Respond("No configurable properties");
                return;
            }

            var msgBuilder = new StringBuilder();
            msgBuilder.AppendLine("Config properties:");
            foreach (var (property, index) in properties.Indexed())
            {
                var name = "no name";
                var description = "no description";

                if (property.TryGetAttribute<DisplayAttribute>(out var display))
                {
                    name = display.Name.OrNull() ?? name;
                    description = display.Description.OrNull() ?? description;
                }

                msgBuilder.AppendLine($"> {index} {property.Name} -- {name}; {description}");
            }

            msgBuilder.AppendLine("> all -- Show the value of all configurable properties");

            self.Context.Respond(msgBuilder.ToString());
        }

        static IEnumerable<PropertyInfo> GetConfigurableProperties(object config, MyPromoteLevel promoLevel)
        {
            var properties = config.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (!IsParseablePrimitive(property.PropertyType)) continue;
                if (property.GetSetMethod() == null) continue;
                if (property.HasAttribute<ConfigPropertyIgnoreAttribute>()) continue;

                if (property.TryGetAttribute(out ConfigPropertyAttribute prop))
                {
                    if (!prop.IsVisibleTo(promoLevel)) continue;
                }

                yield return property;
            }
        }

        static bool IsParseablePrimitive(Type type)
        {
            if (type == typeof(string)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(ulong)) return true;
            return false;
        }

        static object ParsePrimitive(Type type, string value)
        {
            if (type == typeof(string)) return value;
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value);
            if (type == typeof(double)) return double.Parse(value);
            if (type == typeof(long)) return long.Parse(value);
            if (type == typeof(ulong)) return ulong.Parse(value);
            throw new ArgumentException($"unsupported type: {type}");
        }

        public static void ShowUrl(this CommandModule self, string url)
        {
            if (self.Context.Player?.IdentityId is { } playerId)
            {
                self.Context.Respond("Opening wiki on the steam overlay");
                var steamOverlayUrl = MakeSteamOverlayUrl(url);
                MyVisualScriptLogicProvider.OpenSteamOverlay(steamOverlayUrl, playerId);
            }
            else if (self.Context.GetType() == typeof(ConsoleCommandContext))
            {
                self.Context.Respond("Opening wiki on the default web browser");
                Process.Start(url);
            }
            else
            {
                self.Context.Respond(url);
            }

            static string MakeSteamOverlayUrl(string baseUrl)
            {
                const string steamOverlayFormat = "https://steamcommunity.com/linkfilter/?url={0}";
                return string.Format(steamOverlayFormat, baseUrl);
            }
        }
    }
}