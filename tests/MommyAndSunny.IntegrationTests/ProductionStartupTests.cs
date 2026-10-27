using System.Diagnostics;

namespace MommyAndSunny.IntegrationTests;

public class ProductionStartupTests
{
    [Theory]
    [InlineData("disabled", true)]
    [InlineData("omitted", true)]
    [InlineData("enabled", true)]
    [InlineData("partial", false)]
    [InlineData("missing-bank", false)]
    [InlineData("missing-account", false)]
    [InlineData("mock", false)]
    [InlineData("smtp-host", false)]
    [InlineData("smtp-from", false)]
    [InlineData("keys", false)]
    [InlineData("jwt", false)]
    [InlineData("origin", false)]
    [InlineData("proxy", false)]
    public async Task RealHostEnforcesProductionConfiguration(string scenario, bool shouldStart)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MommyAndSunny.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(root.FullName, "src", "MommyAndSunny.API", "bin", configuration, "net10.0", "MommyAndSunny.API.dll");
        var temp = Directory.CreateTempSubdirectory("mommy-startup-");
        try
        {
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = temp.FullName,
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add(dll);
            // Inherit no deployment credentials/configuration from the test runner.
            start.Environment.Clear();
            foreach (var key in new[] { "PATH", "SystemRoot", "TEMP", "TMP", "DOTNET_ROOT" })
                if (Environment.GetEnvironmentVariable(key) is { } value) start.Environment[key] = value;
            var values = new Dictionary<string, string> {
                ["ASPNETCORE_ENVIRONMENT"] = "Production", ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
                ["DOTNET_PROCESSOR_COUNT"] = "2", ["DOTNET_DbgEnableMiniDump"] = "0",
                ["Frontend__BaseUrl"] = "https://example.invalid",
                ["Authentication__DataProtectionKeysPath"] = Path.Combine(temp.FullName, "keys"),
                ["ConnectionStrings__DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=startup_fixture;Username=unused;Password=unused;Timeout=1",
                ["Database__ApplyMigrationsOnStartup"] = "false",
                ["Jwt__Secret"] = "fixture-only-signing-key-00000000000000", ["Jwt__Issuer"] = "fixture", ["Jwt__Audience"] = "fixture",
                ["Smtp__Host"] = "smtp.example.invalid", ["Smtp__FromEmail"] = "fixture@example.invalid",
                ["SePay__Enabled"] = "false", ["SePay__UseMock"] = "false", ["ReverseProxy__KnownProxies"] = "127.0.0.1"
            };
            if (scenario is "enabled" or "partial" or "missing-bank" or "missing-account") {
                values["SePay__Enabled"] = "true"; values["SePay__Bank"] = "fixture";
                values["SePay__AccountNumber"] = "fixture";
                if (scenario != "partial") values["SePay__WebhookApiKey"] = "fixture";
            }
            switch (scenario) {
                case "omitted": values.Remove("SePay__Enabled"); break;
                case "missing-bank": values["SePay__Bank"] = " "; break;
                case "missing-account": values["SePay__AccountNumber"] = ""; break;
                case "mock": values["SePay__UseMock"] = "true"; break;
                case "smtp-host": values["Smtp__Host"] = ""; break;
                case "smtp-from": values["Smtp__FromEmail"] = ""; break;
                case "keys": values["Authentication__DataProtectionKeysPath"] = ""; break;
                case "jwt": values["Jwt__Secret"] = "short"; break;
                case "origin": values["Frontend__BaseUrl"] = "http://example.invalid"; break;
                case "proxy": values["ReverseProxy__KnownProxies"] = "invalid-address"; break;
            }
            foreach (var pair in values) start.Environment[pair.Key] = pair.Value;
            using var process = Process.Start(start)!;
            var listening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var fatal = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var diagnostics = new System.Text.StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data?.Contains("Now listening on:") == true) listening.TrySetResult(); };
            process.BeginOutputReadLine();
            process.ErrorDataReceived += (_, e) => {
                if (e.Data is not { } line) return;
                lock (diagnostics) diagnostics.AppendLine(line);
                // Windows can wait for crash reporting after a fatal startup exception.
                if (line.StartsWith("Unhandled exception.")) fatal.TrySetResult(line);
            };
            process.BeginErrorReadLine();
            try {
                var exited = process.WaitForExitAsync();
                var result = await Task.WhenAny(listening.Task, exited, fatal.Task, Task.Delay(TimeSpan.FromSeconds(15)));
                string details;
                lock (diagnostics) details = diagnostics.ToString();
                if (shouldStart) Assert.True(result == listening.Task, "Host did not listen. " + details);
                else {
                    Assert.True(result == exited || result == fatal.Task, "Host did not reject configuration. " + details);
                    if (result == exited) Assert.NotEqual(0, process.ExitCode);
                    var expected = scenario switch {
                        "partial" or "missing-bank" or "missing-account" => "Enabled SePay requires",
                        "mock" => "SePay mock mode", "smtp-host" => "Production requires real SMTP",
                        "smtp-from" => "Smtp:FromEmail", "keys" => "DataProtectionKeysPath",
                        "jwt" => "Jwt:Secret", "origin" => "Frontend:BaseUrl", "proxy" => "IP address",
                        _ => throw new InvalidOperationException(scenario)
                    };
                    Assert.Contains(expected, details, StringComparison.OrdinalIgnoreCase);
                    Assert.False(listening.Task.IsCompleted);
                }
            }
            finally { if (!process.HasExited) process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); process.WaitForExit(); }
        }
        finally { temp.Delete(recursive: true); }
    }
}
