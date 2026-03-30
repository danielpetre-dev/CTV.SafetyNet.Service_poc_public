using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CTV.SafetyNet.Service_poc.Services.Analyzers
{
	public class FreezeFrameChecker : IVideoChecker
	{
		private readonly FfmpegService _ffmpegService;

		public FreezeFrameChecker(FfmpegService ffmpegService)
		{
			_ffmpegService = ffmpegService;
		}

		public string Name => "Freeze Frames";
		public bool CanFix => false;
		public int Order => 60;

		private static readonly Regex FreezeStartRegex =
			new(@"lavfi\.freezedetect\.freeze_start:\s*(?<start>[0-9.]+)", RegexOptions.Compiled);

		private static readonly Regex FreezeEndRegex =
			new(@"lavfi\.freezedetect\.freeze_end:\s*(?<end>[0-9.]+)", RegexOptions.Compiled);

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var args =
				$"-i \"{status.CurrentFilePath}\" -vf freezedetect=n=0.001:d=0.5 -an -f null -";

			var result = await _ffmpegService.RunAsync(args, ct);

			var starts = FreezeStartRegex.Matches(result.StdErr)
				.Select(m => double.Parse(m.Groups["start"].Value, CultureInfo.InvariantCulture))
				.ToList();

			var ends = FreezeEndRegex.Matches(result.StdErr)
				.Select(m => double.Parse(m.Groups["end"].Value, CultureInfo.InvariantCulture))
				.ToList();

			int freezeCount = Math.Min(starts.Count, ends.Count);
			bool passed = freezeCount == 0;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? "No freeze frames detected."
					: $"Freeze frames detected: {freezeCount}",
				Details = new Dictionary<string, object>
				{
					["FreezeCount"] = freezeCount
				}
			};
		}

		public Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			return Task.FromResult(new VideoPatch
			{
				CheckName = Name,
				Attempted = false,
				Succeeded = false,
				Message = "Freeze frames are not safely auto-fixable.",
				InputFilePath = status.CurrentFilePath
			});
		}
	}
}