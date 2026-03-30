using CTV.SafetyNet.Service_poc.Helpers;
using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using Microsoft.Extensions.DependencyInjection;

namespace CTV.SafetyNet.Service_poc.Services
{
	public class VideoAnalyzer
	{
		private readonly IEnumerable<IVideoChecker> _videoCheckers;

		public VideoAnalyzer(IServiceProvider serviceProvider)
		{
			_videoCheckers = serviceProvider.GetServices<IVideoChecker>();
		}

		public async Task<VideoAnalysisResult> AnalyzeAsync(string filePath, CancellationToken ct = default)
		{
			var status = new VideoStatus
			{
				OriginalFilePath = filePath,
				CurrentFilePath = filePath
			};

			// Step 1: extract metadata first
			status.GeneralInfo = await VideoGeneralInfoExtractor.ExtractAsync(status.CurrentFilePath, ct);

			// Step 2: run checks
			foreach (var checker in _videoCheckers)
			{
				var check = await checker.CheckAsync(status, ct);

				if (check.Passed)
					continue;
				else
					status.Issues.Add(check);

				if (!checker.CanFix)
				{
					status.Patches.Add(new VideoPatch
					{
						CheckName = checker.Name,
						Attempted = false,
						Succeeded = false,
						Message = "Check failed and no auto-fix is available.",
						InputFilePath = status.CurrentFilePath
					});

					continue;
				}

				var patch = await checker.FixAsync(status, ct);
				status.Patches.Add(patch);

				if (patch.Succeeded && !string.IsNullOrWhiteSpace(patch.OutputFilePath))
				{
					status.CurrentFilePath = patch.OutputFilePath;

					// refresh general info after a successful patch
					status.GeneralInfo = await VideoGeneralInfoExtractor.ExtractAsync(status.CurrentFilePath, ct);

					// optional recheck
					var recheck = await checker.CheckAsync(status, ct);
					recheck.CheckName = $"{checker.Name} (recheck)";

					if (!recheck.Passed)
						status.Issues.Add(recheck);
				}
			}

			byte[]? finalBytes = null;
			if (File.Exists(status.CurrentFilePath))
			{
				finalBytes = await File.ReadAllBytesAsync(status.CurrentFilePath, ct);
			}

			return new VideoAnalysisResult
			{
				OriginalFilePath = status.OriginalFilePath,
				FinalFilePath = status.CurrentFilePath,
				FinalVideoBytes = finalBytes,
				GeneralInfo = status.GeneralInfo,
				Issues = status.Issues,
				Patches = status.Patches
			};
		}
	}
}
