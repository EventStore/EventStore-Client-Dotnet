#if GRPC_NETSTANDARD
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Grpc.Core.Logging;

namespace EventStore.Client {
	internal class GrpcCoreSerilogLogger : ILogger {
		private readonly Serilog.ILogger _inner;

		public GrpcCoreSerilogLogger(Serilog.ILogger inner) => _inner = inner;
		public ILogger ForType<T>() => new GrpcCoreSerilogLogger(_inner.ForContext<T>());
		public void Debug(string format, params object[] formatArgs) => _inner.Debug(format, formatArgs);
		public void Info(string format, params object[] formatArgs) => _inner.Information(format, formatArgs);
		public void Warning(string format, params object[] formatArgs) => _inner.Warning(format, formatArgs);
		public void Error(string format, params object[] formatArgs) => _inner.Error(format, formatArgs);

		public void Debug(string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Debug(format, formatArgs);
		}

		public void Info(string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Information(format, formatArgs);
		}

		public void Warning(string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Warning(format, formatArgs);
		}

		public void Warning(Exception exception, string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Warning(exception, format, formatArgs);
		}

		public void Error(string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Error(format, formatArgs);
		}

		public void Error(Exception exception, string message) {
			var (format, formatArgs) = ConvertToMessageTemplate(message);
			_inner.Error(exception, format, formatArgs);
		}

		private static (string, object[]) ConvertToMessageTemplate(string rawMessage) {
			var index = rawMessage.IndexOf(':');
			if (index < 0) {
				return (rawMessage, Array.Empty<object>());
			}
			var sourceFileRange = 16..index;
			var lineNumberRange =
				(sourceFileRange.End.Value + 1)..rawMessage.IndexOf(':', sourceFileRange.End.Value + 1);
			var messageRange = (rawMessage.LastIndexOf(':') + 2)..;
			var propertiesRange = (lineNumberRange.End.Value + 2)..(messageRange.Start.Value - 2);

			var sourceFile = rawMessage[sourceFileRange];
			var lineNumber = Convert.ToInt32(rawMessage[lineNumberRange]);
			var message = rawMessage[messageRange];

			var properties = propertiesRange.End.Value <= propertiesRange.Start.Value
				? (IDictionary<string, string>)ImmutableDictionary<string, string>.Empty
				: rawMessage[propertiesRange].Split(' ')
					.Select(pair => pair.Split('='))
					.ToDictionary(parts => parts[0], parts => parts[1]);

			var messageTemplateBuilder = new StringBuilder("{sourceFile}:{lineNumber} ");
			var formatArgs = new object[3 + properties.Count];
			formatArgs[0] = sourceFile;
			formatArgs[1] = lineNumber;
			var i = 0;
			using var enumerator = properties.GetEnumerator();
			while (i < properties.Count) {
				enumerator.MoveNext();
				formatArgs[i + 2] = enumerator.Current.Value;
				messageTemplateBuilder.Append($"{enumerator.Current.Key}={{{enumerator.Current.Key}}} ");
				i++;
			}

			messageTemplateBuilder.Append(message);
			formatArgs[^1] = rawMessage;

			return (messageTemplateBuilder.ToString(), formatArgs);
		}
	}
}
#endif
