using System.Diagnostics;
using System.Text;

namespace CTV.SafetyNet.Service_poc.Services
{
	public class ProcessResult
	{
		public int ExitCode { get; set; }
		public string StdOut { get; set; } = string.Empty;
		public string StdErr { get; set; } = string.Empty;
		public bool Success => ExitCode == 0;
	}

	public static class ProcessRunner
	{
		public static async Task<ProcessResult> RunAsync(
			string fileName,
			string arguments,
			CancellationToken ct = default)
		{
			var psi = new ProcessStartInfo
			{
				FileName = Path.Combine("Tools", fileName),
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = new Process
			{
				StartInfo = psi
			};

			var stdOut = new StringBuilder();
			var stdErr = new StringBuilder();

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data != null)
					stdOut.AppendLine(e.Data);
			};

			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					stdErr.AppendLine(e.Data);
			};

			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				await process.WaitForExitAsync(ct);

				return new ProcessResult
				{
					ExitCode = process.ExitCode,
					StdOut = stdOut.ToString(),
					StdErr = stdErr.ToString()
				};
			}
			catch (Exception ex)
			{
				stdErr.AppendLine(ex.ToString());
				return new ProcessResult
				{
					ExitCode = process.ExitCode,
					StdOut = stdOut.ToString(),
					StdErr = stdErr.ToString()
				};
			}
		}
	}
}