using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using zulip_cs_cli;
using zulip_cs_lib.Resources;

namespace zulip_set_lib.tests;

/// <summary>Offline regressions for production command parsing and configuration discovery.</summary>
public class CliCommandTests
{
    /// <summary>Rejects invalid flag operations before configuration or client access.</summary>
    /// <param name="value">The invalid operation token.</param>
    [Theory]
    [InlineData("typo")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("add,remove")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" add")]
    [InlineData("remove ")]
    public async Task UpdateFlags_InvalidOperation_DoesNotInvokeAction(string value)
    {
        await AssertInvalidInvocation(
            ["update-flags", "--message-ids", "1", "--flag", "read", "--op", value], "--op");
    }

    /// <summary>Rejects omission of the required flag operation.</summary>
    [Fact]
    public async Task UpdateFlags_MissingOperation_DoesNotInvokeAction()
    {
        await AssertInvalidInvocation(["update-flags", "--message-ids", "1", "--flag", "read"], "--op");
    }

    /// <summary>Accepts only supported case-insensitive operation names.</summary>
    /// <param name="value">The operation token.</param>
    /// <param name="expected">The expected typed operation.</param>
    [Theory]
    [InlineData("add", Messages.FlagOperation.Add)]
    [InlineData("ADD", Messages.FlagOperation.Add)]
    [InlineData("remove", Messages.FlagOperation.Remove)]
    [InlineData("ReMoVe", Messages.FlagOperation.Remove)]
    public void UpdateFlags_ValidOperation_ParsesTypedValue(string value, Messages.FlagOperation expected)
    {
        RootCommand root = Program.BuildRootCommand();
        Option<Messages.FlagOperation> option = GetOption<Messages.FlagOperation>(root, "update-flags", "--op");

        ParseResult result = root.Parse(["update-flags", "--message-ids", "1", "--flag", "read", "--op", value]);

        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.GetValue(option));
    }

    /// <summary>Rejects invalid propagation rather than silently choosing One.</summary>
    /// <param name="value">The invalid propagation token.</param>
    [Theory]
    [InlineData("typo")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("one,later")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" one")]
    [InlineData("all ")]
    public async Task EditMessage_InvalidPropagation_DoesNotInvokeAction(string value)
    {
        await AssertInvalidInvocation(["edit-message", "--message-id", "1", "--propagate-mode", value], "--propagate-mode");
    }

    /// <summary>Accepts case-insensitive propagation names and retains the omitted One default.</summary>
    /// <param name="value">The propagation token, or null to omit the option.</param>
    /// <param name="expected">The expected typed propagation mode.</param>
    [Theory]
    [InlineData(null, Messages.EditPropagateMode.One)]
    [InlineData("one", Messages.EditPropagateMode.One)]
    [InlineData("ONE", Messages.EditPropagateMode.One)]
    [InlineData("later", Messages.EditPropagateMode.Later)]
    [InlineData("LaTeR", Messages.EditPropagateMode.Later)]
    [InlineData("all", Messages.EditPropagateMode.All)]
    [InlineData("ALL", Messages.EditPropagateMode.All)]
    public void EditMessage_ValidPropagation_ParsesTypedValue(string? value, Messages.EditPropagateMode expected)
    {
        RootCommand root = Program.BuildRootCommand();
        Option<Messages.EditPropagateMode> option = GetOption<Messages.EditPropagateMode>(root, "edit-message", "--propagate-mode");
        string[] arguments = value == null
            ? ["edit-message", "--message-id", "1"]
            : ["edit-message", "--message-id", "1", "--propagate-mode", value];

        ParseResult result = root.Parse(arguments);

        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.GetValue(option));
    }

    /// <summary>Rejects malformed or overflowing stream IDs without losing the supplied filter.</summary>
    /// <param name="value">The invalid stream ID token.</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854775809")]
    [InlineData("1.5")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetMessages_InvalidStreamId_DoesNotInvokeAction(string value)
    {
        await AssertInvalidInvocation(["get-messages", "--stream-id", value], "--stream-id");
    }

    /// <summary>Preserves omission and the full signed Int64 range for stream filters.</summary>
    /// <param name="value">The stream ID token, or null to omit the filter.</param>
    /// <param name="expected">The expected nullable stream ID.</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData("0", 0L)]
    [InlineData("42", 42L)]
    [InlineData("-1", -1L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("-9223372036854775808", long.MinValue)]
    public void GetMessages_ValidStreamId_ParsesTypedValue(string? value, long? expected)
    {
        RootCommand root = Program.BuildRootCommand();
        Option<long?> option = GetOption<long?>(root, "get-messages", "--stream-id");
        string[] arguments = value == null ? ["get-messages"] : ["get-messages", "--stream-id", value];

        ParseResult result = root.Parse(arguments);

        Assert.Empty(result.Errors);
        Assert.Equal(expected, result.GetValue(option));
    }

    /// <summary>Rejects a supplied option without its argument, including before another option.</summary>
    /// <param name="commandName">The command to parse.</param>
    /// <param name="optionName">The option missing its argument.</param>
    /// <param name="followedByOption">Whether another option follows the missing argument.</param>
    [Theory]
    [InlineData("update-flags", "--op", false)]
    [InlineData("update-flags", "--op", true)]
    [InlineData("edit-message", "--propagate-mode", false)]
    [InlineData("edit-message", "--propagate-mode", true)]
    [InlineData("get-messages", "--stream-id", false)]
    [InlineData("get-messages", "--stream-id", true)]
    public async Task SuppliedOption_MissingArgument_DoesNotInvokeAction(
        string commandName, string optionName, bool followedByOption)
    {
        string[] arguments = commandName switch
        {
            "update-flags" => ["update-flags", "--message-ids", "1", "--flag", "read", optionName],
            "edit-message" => ["edit-message", "--message-id", "1", optionName],
            _ => ["get-messages", optionName],
        };
        if (followedByOption)
        {
            arguments = [.. arguments, "--zuliprc", "another-missing-file.zuliprc"];
        }

        await AssertInvalidInvocation(arguments, optionName);
    }

    /// <summary>Searches root candidates from either the root or a nested, optionally terminated path.</summary>
    /// <param name="nested">Whether to begin two directories below the root.</param>
    /// <param name="secrets">Whether the root configuration is in the secrets directory.</param>
    /// <param name="trailingSeparator">Whether the start has a trailing directory separator.</param>
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void FindZulipRC_SearchesTerminalCandidates(bool nested, bool secrets, bool trailingSeparator)
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;
        string parent = Path.Combine(root, "cli-discovery-tests");
        string start = nested ? Path.Combine(parent, "nested") : root;
        string rootFile = Path.Combine(root, "zuliprc");
        string rootSecretsFile = Path.Combine(root, "secrets", "zuliprc");
        string expected = secrets ? rootSecretsFile : rootFile;
        List<string> visited = [];
        List<string> expectedVisits = nested
            ? [Path.Combine(start, "zuliprc"), Path.Combine(start, "secrets", "zuliprc"),
               Path.Combine(parent, "zuliprc"), Path.Combine(parent, "secrets", "zuliprc"), rootFile]
            : [rootFile];
        if (secrets)
        {
            expectedVisits.Add(rootSecretsFile);
        }

        string found = Program.FindZulipRC(
            trailingSeparator ? start + Path.DirectorySeparatorChar : start,
            candidate =>
            {
                visited.Add(candidate);
                return candidate == expected;
            });

        Assert.Equal(expected, found);
        Assert.Equal(expectedVisits, visited);
    }

    /// <summary>Stops after checking both terminal candidates when no configuration exists.</summary>
    /// <param name="nested">Whether the search begins below the root.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindZulipRC_NoCandidates_TerminatesAtRoot(bool nested)
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;
        string start = nested ? Path.Combine(root, "cli-discovery-tests") : root;
        List<string> visited = [];
        string[] expected = nested
            ? [Path.Combine(start, "zuliprc"), Path.Combine(start, "secrets", "zuliprc"),
               Path.Combine(root, "zuliprc"), Path.Combine(root, "secrets", "zuliprc")]
            : [Path.Combine(root, "zuliprc"), Path.Combine(root, "secrets", "zuliprc")];

        Assert.Throws<DirectoryNotFoundException>(() => Program.FindZulipRC(
            start + Path.DirectorySeparatorChar,
            candidate =>
            {
                visited.Add(candidate);
                return false;
            }));

        Assert.Equal(expected, visited);
    }

    /// <summary>Checks the direct configuration before secrets, and current secrets before ancestors.</summary>
    /// <param name="directExists">Whether the starting directory has a direct configuration.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindZulipRC_CurrentCandidates_TakePrecedence(bool directExists)
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;
        string start = Path.Combine(root, "cli-discovery-tests");
        string direct = Path.Combine(start, "zuliprc");
        string secrets = Path.Combine(start, "secrets", "zuliprc");
        List<string> visited = [];

        string found = Program.FindZulipRC(start, candidate =>
        {
            visited.Add(candidate);
            return candidate != direct || directExists;
        });

        Assert.Equal(directExists ? direct : secrets, found);
        Assert.Equal(directExists ? [direct] : new[] { direct, secrets }, visited);
    }

    /// <summary>Gets a typed option from the real production command metadata.</summary>
    /// <param name="root">The production root command.</param>
    /// <param name="commandName">The owning subcommand name.</param>
    /// <param name="optionName">The option name.</param>
    /// <returns>The typed production option.</returns>
    private static Option<T> GetOption<T>(RootCommand root, string commandName, string optionName)
    {
        Command command = Assert.Single(root.Subcommands, candidate => candidate.Name == commandName);
        return Assert.IsType<Option<T>>(Assert.Single(command.Options, candidate => candidate.Name == optionName));
    }

    /// <summary>Verifies parse errors and nonzero invocation without permitting action exceptions.</summary>
    /// <param name="arguments">The invalid command arguments.</param>
    /// <param name="optionName">The option whose diagnostic must be present.</param>
    private static async Task AssertInvalidInvocation(string[] arguments, string optionName)
    {
        RootCommand root = Program.BuildRootCommand();
        string nonexistentConfig = Path.Combine(Directory.GetCurrentDirectory(), $"missing-{Guid.NewGuid():N}.zuliprc");
        ParseResult result = root.Parse(["--zuliprc", nonexistentConfig, .. arguments]);
        using StringWriter output = new();
        using StringWriter error = new();
        InvocationConfiguration configuration = new()
        {
            Output = output,
            Error = error,
            EnableDefaultExceptionHandler = false,
        };

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, parseError => parseError.Message.Contains(optionName, StringComparison.Ordinal));
        int exitCode = await result.InvokeAsync(configuration);

        Assert.NotEqual(0, exitCode);
        Assert.Contains(optionName, error.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Could not find file", error.ToString(), StringComparison.Ordinal);
    }
}
