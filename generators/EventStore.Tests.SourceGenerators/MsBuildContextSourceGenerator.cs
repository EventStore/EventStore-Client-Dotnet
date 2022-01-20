using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace EventStore.Client {
	[Generator]
	public class MsBuildContextSourceGenerator : ISourceGenerator {
		public void Initialize(GeneratorInitializationContext context) {
		}

		public void Execute(GeneratorExecutionContext context) {
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace",
				out var rootNamespace);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.ProjectDir",
				out var projectDir);

			var sourceText = SourceText.From(@$"
namespace {rootNamespace} {{
    public static class ProjectDir {{
		public static readonly string Current = ""{projectDir}"";
	}}
}}", Encoding.UTF8);

			context.AddSource("ProjectDir.cs", sourceText); ;
		}
	}
}
