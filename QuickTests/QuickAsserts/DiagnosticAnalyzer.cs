using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace QuickAsserts
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class QuickAssertsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "QuickAsserts";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = "Can create assertions";
        private static readonly LocalizableString MessageFormat = "Create assertions";
        private static readonly LocalizableString Description = "Create assertions";
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.LocalDeclarationStatement, SyntaxKind.CastExpression);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = (LocalDeclarationStatementSyntax) context.Node;

            if (localDeclaration.Declaration.Variables.Any(variable => variable.Initializer == null))
                return;

            var node = context.Node;

            while (node.Kind() != SyntaxKind.MethodDeclaration)
            {
                node = node.Parent;
            }

            var method = (MethodDeclarationSyntax) node;

            if (
                method.AttributeLists.Any(
                    x =>
                        x.Attributes.Any(
                            y =>
                                y.Name is IdentifierNameSyntax &&
                                ((IdentifierNameSyntax) y.Name).Identifier.Text.ToLower().Contains("test"))))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }
    }
}
