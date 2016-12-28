using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace QuickAsserts
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QuickAssertsCodeFixProvider)), Shared]
    public partial class QuickAssertsCodeFixProvider : CodeFixProvider
    {
        private const string title = "Create assertions";

        private List<ExpressionStatementSyntax> _asserts;

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(QuickAssertsAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddAssertionsAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> AddAssertionsAsync(Document document, LocalDeclarationStatementSyntax localDecl, CancellationToken cancellationToken)
        {
            _asserts = new List<ExpressionStatementSyntax>();
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var symbolInfo = semanticModel.GetSymbolInfo(localDecl.Declaration.Type);
            var typeSymbol = symbolInfo.Symbol;

            var visitor = new Visitor();
            visitor.ParentName = localDecl.Declaration.Variables.FirstOrDefault().Identifier.Text;
            visitor.Visit(typeSymbol);

            CreateAsserts(visitor.Properties);

            editor.InsertAfter(localDecl.Parent.ChildNodes().LastOrDefault(), _asserts);
            var newDocument = editor.GetChangedDocument();

            return newDocument;
        }

        private void CreateAsserts(IEnumerable<ReflectedProperty> properties)
        {
            var assert = SyntaxFactory.IdentifierName("Assert");
            var areEqual = SyntaxFactory.IdentifierName("AreEqual");

            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, assert,
                areEqual);

            foreach (var property in properties)
            {
                var names = property.Name.Split('.');

                SeparatedSyntaxList<ArgumentSyntax> argumentList;

                if (names.Length > 1)
                {
                    MemberAccessExpressionSyntax outer = null;
                    for (var i = 0; i < names.Length; i++)
                    {
                        if (outer == null)
                        {
                            outer = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(names[i]),
                                SyntaxFactory.IdentifierName(names[i + 1]));
                            i++;
                        }
                        else
                        {
                            var member = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                outer,
                                SyntaxFactory.IdentifierName(names[i]));
                            outer = member;
                        }
                    }

                    argumentList = SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Argument(GetArgument(property.Type)),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(outer)
                    });
                }
                else
                {
                    var first =
                        memberAccess.WithLeadingTrivia(
                            SyntaxFactory.TriviaList(new[]
                                {SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, string.Empty)}));

                    argumentList = SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Argument(GetArgument(property.Type)),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(names[0]))
                    });

                    _asserts.Add(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            first,
                            SyntaxFactory.ArgumentList(argumentList))));

                    continue;
                }

                _asserts.Add(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            memberAccess,
                            SyntaxFactory.ArgumentList(argumentList))));
            }
        }

        private ExpressionSyntax GetArgument(string propertyType)
        {
            if (propertyType == "Int32" || propertyType == "Int64" || propertyType == "Int16" || propertyType == "Enum")
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0));

            if (propertyType == "DateTime")
                return
                    SyntaxFactory.ObjectCreationExpression(
                       SyntaxFactory.IdentifierName("DateTime"),
                       SyntaxFactory.ArgumentList(
                           SyntaxFactory.SeparatedList(
                               new[]
                               {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(DateTime.Now.Year))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(1))),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(1)))
                               }))
                       ,
                       null)
                    .WithAdditionalAnnotations(
                        Formatter.Annotation
                    );

            if (propertyType == "Boolean")
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.FalseLiteralExpression);

            if (propertyType == "String")
                return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(""));

            return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NullLiteralExpression);
        }
    }
}