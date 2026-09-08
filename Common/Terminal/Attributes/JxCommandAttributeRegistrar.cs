using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JxModule.Terminal
{
    public static class JxCommandAttributeRegistrar
    {
        private const BindingFlags StaticMethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceMethodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void RegisterStaticCommands(JxTerminal terminal, params Type[] types)
        {
            if (terminal == null)
            {
                throw new ArgumentNullException(nameof(terminal));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var methods = types
                .Where(type => type != null)
                .SelectMany(type => type.GetMethods(StaticMethodFlags)
                    .Select(method => new JxAttributedCommandMethod(null, method)))
                .ToArray();

            RegisterMethods(terminal, methods);
        }

        public static void RegisterCommands(JxTerminal terminal, object target)
        {
            if (terminal == null)
            {
                throw new ArgumentNullException(nameof(terminal));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var methods = target
                .GetType()
                .GetMethods(InstanceMethodFlags)
                .Select(method => new JxAttributedCommandMethod(target, method))
                .ToArray();

            RegisterMethods(terminal, methods);
        }

        private static void RegisterMethods(JxTerminal terminal, IReadOnlyList<JxAttributedCommandMethod> methods)
        {
            var commandMethods = methods
                .Where(method => method.Method.GetCustomAttribute<JxCommandAttribute>() != null)
                .ToArray();

            var optionProviders = commandMethods
                .Where(method => IsStringEnumerable(method.Method.ReturnType) && method.Method.GetParameters().Length == 0)
                .ToDictionary(method => GetCommandKey(method.Method), method => CreateOptionProvider(method.Target, method.Method));

            foreach (var commandMethod in commandMethods)
            {
                var attribute = commandMethod.Method.GetCustomAttribute<JxCommandAttribute>();
                var completionProvider = CreateCompletionProvider(commandMethod.Method, optionProviders);
                var descriptor = new JxCommandDescriptor(
                    GetCommandKey(commandMethod.Method),
                    context => ExecuteMethod(commandMethod.Target, commandMethod.Method, context),
                    attribute.Description,
                    completionProvider);

                terminal.Register(descriptor);
            }
        }

        private static IJxCommandCompletionProvider CreateCompletionProvider(MethodInfo method, IReadOnlyDictionary<string, Func<IEnumerable<string>>> optionProviders)
        {
            var parameters = method.GetParameters();
            if (parameters.All(parameter => parameter.GetCustomAttribute<JxOptionValueAttribute>() == null))
            {
                return null;
            }

            return new JxAttributeCommandCompletionProvider(parameters, optionProviders);
        }

        private static JxCommandResult ExecuteMethod(object target, MethodInfo method, JxCommandContext context)
        {
            try
            {
                var arguments = BuildArguments(method, context);
                var result = method.Invoke(target, arguments);
                return ConvertReturnValue(result, method.ReturnType);
            }
            catch (TargetInvocationException e)
            {
                return JxCommandResult.Failure(e.InnerException?.Message ?? e.Message);
            }
            catch (Exception e)
            {
                return JxCommandResult.Failure(e.Message);
            }
        }

        private static object[] BuildArguments(MethodInfo method, JxCommandContext context)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(JxCommandContext))
            {
                return new object[] { context };
            }

            if (context.Arguments.Count > parameters.Length)
            {
                throw new ArgumentException($"Too many arguments. Expected {parameters.Length}, got {context.Arguments.Count}.");
            }

            var arguments = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i >= context.Arguments.Count)
                {
                    if (parameters[i].HasDefaultValue)
                    {
                        arguments[i] = parameters[i].DefaultValue;
                        continue;
                    }

                    throw new ArgumentException($"Missing argument: {parameters[i].Name}");
                }

                arguments[i] = ConvertArgument(context.Arguments[i], parameters[i].ParameterType, parameters[i].Name);
            }

            return arguments;
        }

        private static object ConvertArgument(string value, Type targetType, string parameterName)
        {
            if (targetType == typeof(string))
            {
                return value;
            }

            if (targetType == typeof(int))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            else if (targetType == typeof(long))
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            else if (targetType == typeof(float))
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            else if (targetType == typeof(double))
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
            }
            else if (targetType == typeof(bool))
            {
                if (bool.TryParse(value, out var result))
                {
                    return result;
                }
            }
            else if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out var result))
                {
                    return result;
                }
            }

            throw new ArgumentException($"Invalid argument '{value}' for {parameterName}. Expected {targetType.Name}.");
        }

        private static JxCommandResult ConvertReturnValue(object result, Type returnType)
        {
            if (returnType == typeof(void) || result == null)
            {
                return JxCommandResult.Success();
            }

            if (result is JxCommandResult commandResult)
            {
                return commandResult;
            }

            if (result is string message)
            {
                return JxCommandResult.Success(message);
            }

            if (result is bool isSuccess)
            {
                return isSuccess ? JxCommandResult.Success() : JxCommandResult.Failure();
            }

            if (result is IEnumerable<string> values)
            {
                return JxCommandResult.Success(string.Join(Environment.NewLine, values));
            }

            return JxCommandResult.Success(result.ToString());
        }

        private static Func<IEnumerable<string>> CreateOptionProvider(object target, MethodInfo method)
        {
            return () =>
            {
                var result = method.Invoke(target, null);
                return result as IEnumerable<string> ?? Enumerable.Empty<string>();
            };
        }

        private static string GetCommandKey(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<JxCommandAttribute>();
            if (!string.IsNullOrWhiteSpace(attribute.Key))
            {
                return attribute.Key.Trim();
            }

            return ToKebabCase(method.Name);
        }

        private static bool IsStringEnumerable(Type type)
        {
            return type != typeof(string) && typeof(IEnumerable<string>).IsAssignableFrom(type);
        }

        private static string ToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (char.IsUpper(character))
                {
                    if (i > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

        private readonly struct JxAttributedCommandMethod
        {
            public object Target { get; }
            public MethodInfo Method { get; }

            public JxAttributedCommandMethod(object target, MethodInfo method)
            {
                Target = target;
                Method = method;
            }
        }

        private sealed class JxAttributeCommandCompletionProvider : IJxCommandCompletionProvider
        {
            private readonly ParameterInfo[] _parameters;
            private readonly IReadOnlyDictionary<string, Func<IEnumerable<string>>> _optionProviders;

            public JxAttributeCommandCompletionProvider(
                ParameterInfo[] parameters,
                IReadOnlyDictionary<string, Func<IEnumerable<string>>> optionProviders)
            {
                _parameters = parameters;
                _optionProviders = optionProviders;
            }

            public IEnumerable<string> Complete(JxCommandCompletionContext context)
            {
                if (context.CurrentArgumentIndex < 0 || context.CurrentArgumentIndex >= _parameters.Length)
                {
                    return Enumerable.Empty<string>();
                }

                var optionValueAttribute = _parameters[context.CurrentArgumentIndex].GetCustomAttribute<JxOptionValueAttribute>();
                if (optionValueAttribute == null || string.IsNullOrWhiteSpace(optionValueAttribute.ProviderKey))
                {
                    return Enumerable.Empty<string>();
                }

                if (!_optionProviders.TryGetValue(optionValueAttribute.ProviderKey, out var provider))
                {
                    return Enumerable.Empty<string>();
                }

                return provider.Invoke() ?? Enumerable.Empty<string>();
            }
        }
    }
}
