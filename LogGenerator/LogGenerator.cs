using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace LogGenerator
{
    [Generator]
    public sealed class LogGenerator : ISourceGenerator
    {
        private const string LogSource = "MetaMystia.Plugin.Instance.Log";
        private const string LogSourceType = "BepInEx.Logging.ManualLogSource";
        private const string TargetNamespace = "MetaMystia";

        public void Initialize(GeneratorInitializationContext context)
        {
            // 调试用
            // System.Diagnostics.Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var root = tree.GetRoot(context.CancellationToken);

                foreach (var cls in root.DescendantNodes()
                                        .OfType<ClassDeclarationSyntax>())
                {
                    if (!cls.Modifiers.Any(SyntaxKind.PartialKeyword))
                        continue;

                    if (!HasAutoLog(cls))
                        continue;

                    var className = cls.Identifier.Text;

                    // var ns = GetNamespace(cls);
                    var model = context.Compilation.GetSemanticModel(cls.SyntaxTree);
                    var clsSymbol = model.GetDeclaredSymbol(cls);
                    if (clsSymbol == null) continue;
                    string fullClassName = clsSymbol.Name;
                    string namespaceName = clsSymbol.ContainingNamespace.IsGlobalNamespace ? "" : clsSymbol.ContainingNamespace.ToString();
                    
                    var source = GeneratePartial(namespaceName, fullClassName);

                    context.AddSource(
                        $"{fullClassName}.bepinexlog.g.cs",
                        SourceText.From(source, Encoding.UTF8)
                    );
                }
            }
        }

        private static bool HasAutoLog(ClassDeclarationSyntax cls)
        {
            return cls.AttributeLists
                      .SelectMany(a => a.Attributes)
                      .Any(a => a.Name.ToString().EndsWith("AutoLog"));
        }

        private static string? GetNamespace(SyntaxNode node)
        {
            while (node != null)
            {
                if (node is NamespaceDeclarationSyntax ns)
                    return ns.Name.ToString();
                node = node.Parent;
            }
            return null;
        }

        private static string GeneratePartial(string? ns, string className)
        {
            var sb = new StringBuilder();

            if (ns != null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"public partial class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"        [MemoryPack.MemoryPackIgnore]");
            sb.AppendLine($"        private static SgrYuki.Utils.LogWrapper Log => new ({LogSource}, nameof({className}));");
            sb.AppendLine("}");

            if (ns != null)
                sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
