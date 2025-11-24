using FlakyTestDetector.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FlakyTestDetector.Analyzers;

/// <summary>
/// Analyzes test code for common flaky test patterns
/// </summary>
public class StaticCodeAnalyzer
{
    public List<string> AnalyzeTestMethod(string sourceCode, string methodName)
    {
        var issues = new List<string>();

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            var methodDeclaration = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);

            if (methodDeclaration == null)
                return issues;

            // Check for DateTime.Now usage
            if (HasDateTimeNowUsage(methodDeclaration))
            {
                issues.Add("Uses DateTime.Now which can cause time-dependent flakiness");
            }

            // Check for Random without seed
            if (HasRandomWithoutSeed(methodDeclaration))
            {
                issues.Add("Uses Random without explicit seed which can cause non-deterministic behavior");
            }

            // Check for Thread.Sleep
            if (HasThreadSleep(methodDeclaration))
            {
                issues.Add("Uses Thread.Sleep which can cause timing-dependent flakiness");
            }

            // Check for Task.Delay
            if (HasTaskDelay(methodDeclaration))
            {
                issues.Add("Uses Task.Delay which can cause timing-dependent flakiness");
            }

            // Check for static or shared mutable state
            if (HasStaticFieldAccess(methodDeclaration))
            {
                issues.Add("Accesses static fields which may cause test order dependencies");
            }

            // Check for file system operations without cleanup
            if (HasFileSystemOperations(methodDeclaration))
            {
                issues.Add("Performs file system operations that may not be properly cleaned up");
            }

            // Check for GUID generation
            if (HasGuidGeneration(methodDeclaration))
            {
                issues.Add("Generates GUIDs which are non-deterministic");
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Error during static analysis: {ex.Message}");
        }

        return issues;
    }

    private bool HasDateTimeNowUsage(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.ToString().Contains("DateTime.Now") || 
                      m.ToString().Contains("DateTime.UtcNow"));
    }

    private bool HasRandomWithoutSeed(MethodDeclarationSyntax method)
    {
        var objectCreations = method.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(o => o.Type.ToString().Contains("Random"));

        foreach (var creation in objectCreations)
        {
            var argumentList = creation.ArgumentList;
            if (argumentList == null || !argumentList.Arguments.Any())
            {
                return true;
            }
        }

        return false;
    }

    private bool HasThreadSleep(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(i => i.ToString().Contains("Thread.Sleep"));
    }

    private bool HasTaskDelay(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(i => i.ToString().Contains("Task.Delay"));
    }

    private bool HasStaticFieldAccess(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(i => char.IsUpper(i.Identifier.Text[0]) && 
                     !i.Identifier.Text.StartsWith("Test"));
    }

    private bool HasFileSystemOperations(MethodDeclarationSyntax method)
    {
        var fileSystemTypes = new[] { "File.", "Directory.", "FileStream", "StreamWriter", "StreamReader" };
        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => i.ToString());

        return invocations.Any(i => fileSystemTypes.Any(fs => i.Contains(fs)));
    }

    private bool HasGuidGeneration(MethodDeclarationSyntax method)
    {
        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(i => i.ToString().Contains("Guid.NewGuid"));
    }

    public List<string> SuggestFixes(List<string> issues)
    {
        var fixes = new List<string>();

        foreach (var issue in issues)
        {
            if (issue.Contains("DateTime.Now"))
            {
                fixes.Add("Use a clock abstraction or inject time as a dependency for testing");
            }
            else if (issue.Contains("Random without explicit seed"))
            {
                fixes.Add("Use Random with a fixed seed (e.g., new Random(0)) for deterministic tests");
            }
            else if (issue.Contains("Thread.Sleep") || issue.Contains("Task.Delay"))
            {
                fixes.Add("Avoid timing-based waits; use proper synchronization mechanisms or mocking");
            }
            else if (issue.Contains("static fields"))
            {
                fixes.Add("Avoid shared mutable state; use test initialization/cleanup methods to reset state");
            }
            else if (issue.Contains("file system operations"))
            {
                fixes.Add("Use TestCleanup attribute to ensure file cleanup, or use in-memory alternatives");
            }
            else if (issue.Contains("GUIDs"))
            {
                fixes.Add("Use deterministic values for testing or inject GUID generator as a dependency");
            }
        }

        return fixes;
    }
}
