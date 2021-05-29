using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NJsonSchema.CodeGeneration.CSharp;

using NSwag;
using NSwag.CodeGeneration.CSharp;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

#pragma warning disable SA1204, SA1009
namespace Brighid.Identity.ClientGenerator
{
    [Generator]
    public class Program : ISourceGenerator
    {
        private readonly string[] usings = new[]
        {
            "System",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Extensions",
            "Brighid.Identity.Client",
        };

        private readonly string[] ignoredCodes = new string[]
        {
            "CS1591",
        };

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            ExecuteAsync(context).GetAwaiter().GetResult();
        }

        public async Task ExecuteAsync(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TemplateDirectory", out var templateDirectory);
            if (templateDirectory == null)
            {
                throw new Exception("Template Directory should be defined.");
            }

            var client = new HttpClient
            {
                DefaultRequestVersion = new Version(2, 0),
            };

            var swaggerString = await client.GetStringAsync("https://identity.brigh.id/swagger/v1/swagger.json");
            var document = await OpenApiDocument.FromJsonAsync(swaggerString);
            var settings = new CSharpClientGeneratorSettings
            {
                GenerateClientClasses = true,
                GenerateClientInterfaces = true,
                ClassName = "{controller}Client",
                OperationNameGenerator = new OperationNameGenerator(),
                CSharpGeneratorSettings =
                {
                    TemplateDirectory = templateDirectory,
                    Namespace = "Brighid.Identity.Client",
                    JsonLibrary = CSharpJsonLibrary.SystemTextJson
                }
            };

            var generator = new CSharpClientGenerator(document, settings);
            var code = generator.GenerateFile();
            var extensions = GenerateServiceCollectionExtensions(code);

            context.AddSource("GeneratedClientCode", code);
            context.AddSource("GeneratedExtensions", extensions);
        }

        public string GenerateServiceCollectionExtensions(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var treeRoot = tree.GetCompilationUnitRoot();
            var interfaces = treeRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var interfaceNames = from iface in interfaces
                                 let name = iface.Identifier.ValueText
                                 where !name.EndsWith("Factory")
                                 select iface.Identifier.ValueText;

            var codes = ignoredCodes.Select(code => ParseExpression(code));
            var ignoreWarningsTrivia = Trivia(PragmaWarningDirectiveTrivia(Token(DisableKeyword), SeparatedList(codes), true));
            var members = GenerateUseMethods(interfaceNames);
            var classDeclaration = ClassDeclaration("ServiceCollectionExtensions")
                .WithModifiers(TokenList(Token(PublicKeyword), Token(StaticKeyword), Token(PartialKeyword)))
                .WithMembers(List(members));

            var namespaceDeclaration = NamespaceDeclaration(ParseName("Microsoft.Extensions.DependencyInjection"))
                .WithMembers(List(new MemberDeclarationSyntax[] { classDeclaration }));

            var usings = List(this.usings.Select(@using => UsingDirective(ParseName(@using))));
            var compilationUnit = CompilationUnit(List<ExternAliasDirectiveSyntax>(), usings, List<AttributeListSyntax>(), List(new MemberDeclarationSyntax[] { namespaceDeclaration }))
                .WithLeadingTrivia(TriviaList(ignoreWarningsTrivia));

            return compilationUnit.NormalizeWhitespace().GetText(Encoding.UTF8).ToString();
        }

        public IEnumerable<MemberDeclarationSyntax> GenerateUseMethods(IEnumerable<string> interfaceNames)
        {
            foreach (var interfaceName in interfaceNames)
            {
                var implementationName = interfaceName[1..];
                yield return GenerateUseMethod(interfaceName, implementationName);
            }
        }

        public MemberDeclarationSyntax GenerateUseMethod(string interfaceName, string implementationName)
        {
            var parameters = SeparatedList(new ParameterSyntax[]
            {
                Parameter(List<AttributeListSyntax>(), TokenList(Token(ThisKeyword)), ParseTypeName("IServiceCollection"), Identifier("services"), null),
            });

            var methodName = $"UseBrighidIdentity{implementationName.Replace("Client", "")}";
            return MethodDeclaration(ParseTypeName("void"), methodName)
                .WithModifiers(TokenList(Token(PublicKeyword), Token(StaticKeyword)))
                .WithParameterList(ParameterList(parameters))
                .WithBody(Block(GenerateUseMethodBody(interfaceName, implementationName)));
        }

        public static IEnumerable<StatementSyntax> GenerateUseMethodBody(string interfaceName, string implementationName)
        {
            yield return ParseStatement("var baseUri = GetIdentityServerApiBaseUri(services);");
            yield return ParseStatement($"services.TryAddSingleton<{interfaceName}, {implementationName}>();");
            yield return ParseStatement($"services.TryAddSingleton<{interfaceName}Factory, {implementationName}Factory>();");
            yield return ParseStatement($"services.UseBrighidIdentity<{interfaceName}, {implementationName}>(baseUri);");
            yield return ParseStatement($"services.UseBrighidIdentity<{interfaceName}Factory, {implementationName}Factory>(baseUri);");
        }
    }
}
