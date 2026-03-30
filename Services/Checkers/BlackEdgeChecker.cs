using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class BlackEdgeChecker : IVideoChecker
	{
		private readonly FfmpegService _ffmpegService;
		private readonly FfprobeService _ffprobeService;

		public BlackEdgeChecker(FfmpegService ffmpegService, FfprobeService ffprobeService)
		{
			_ffmpegService = ffmpegService;
			_ffprobeService = ffprobeService;
		}

		public string Name => "Black Edge Frames";
		public bool CanFix => true;

		private static readonly Regex BlackRegex =
			new(@"black_start:(?<start>[0-9.]+)\s+black_end:(?<end>[0-9.]+)\s+black_duration:(?<dur>[0-9.]+)",
				RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var segments = await DetectBlackSegmentsAsync(status.CurrentFilePath, ct);
			var totalDuration = await GetDurationAsync(status.CurrentFilePath, ct);

			bool hasStartBlack = segments.Any(s => s.Start <= 0.08);
			bool hasEndBlack = segments.Any(s => Math.Abs(s.End - totalDuration) <= 0.08);

			bool passed = !hasStartBlack && !hasEndBlack;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? "No black edge frames detected."
					: $"Black frames detected at edges. StartBlack={hasStartBlack}, EndBlack={hasEndBlack}",
				Details = new Dictionary<string, object>
				{
					["SegmentsFound"] = segments.Count,
					["TotalDuration"] = totalDuration,
					["HasStartBlack"] = hasStartBlack,
					["HasEndBlack"] = hasEndBlack
				}
			};
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var inputPath = status.CurrentFilePath;

			var segments = await DetectBlackSegmentsAsync(inputPath, ct);
			var totalDuration = await GetDurationAsync(inputPath, ct);

			double newStart = 0;
			double newEnd = totalDuration;

			var startBlack = segments
				.Where(s => s.Start <= 0.08)
				.OrderBy(s => s.Start)
				.FirstOrDefault();

			if (startBlack != null)
				newStart = startBlack.End;

			var endBlack = segments
				.Where(s => Math.Abs(s.End - totalDuration) <= 0.08)
				.OrderByDescending(s => s.End)
				.FirstOrDefault();

			if (endBlack != null)
				newEnd = endBlack.Start;

			if (newEnd <= newStart)
			{
				return new VideoPatch
				{
					CheckName = Name,
					Attempted = true,
					Succeeded = false,
					Message = "Calculated invalid trim points.",
					InputFilePath = inputPath
				};
			}

			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_blacktrim_fixed.mp4");

			var trimDuration = newEnd - newStart;

			var args =
				$"-y -ss {newStart.ToString(CultureInfo.InvariantCulture)} " +
				$"-i \"{inputPath}\" -t {trimDuration.ToString(CultureInfo.InvariantCulture)} " +
				"-c:v libx264 -c:a aac -movflags +faststart " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = result.Success,
				Message = result.Success
					? $"Black edge frames trimmed. Start={newStart:F3}s End={newEnd:F3}s"
					: $"Black edge fix failed: {result.StdErr}",
				InputFilePath = inputPath,
				OutputFilePath = result.Success ? outputPath : null
			};
		}

		private async Task<List<BlackSegment>> DetectBlackSegmentsAsync(string filePath, CancellationToken ct)
		{
			var args = $"-i \"{filePath}\" -vf blackdetect=d=0.05:pix_th=0.10 -an -f null -";
			var result = await _ffmpegService.RunAsync(args, ct);

			var matches = BlackRegex.Matches(result.StdErr);
			var list = new List<BlackSegment>();

			foreach (Match match in matches)
			{
				list.Add(new BlackSegment
				{
					Start = double.Parse(match.Groups["start"].Value, CultureInfo.InvariantCulture),
					End = double.Parse(match.Groups["end"].Value, CultureInfo.InvariantCulture)
				});
			}

			return list;
		}

		private async Task<double> GetDurationAsync(string filePath, CancellationToken ct)
		{
			var probe = await _ffprobeService.ProbeAsync(filePath, ct);
			return FfprobeService.ParseDouble(probe.Format?.Duration);
		}

		private sealed class BlackSegment
		{
			public double Start { get; set; }
			public double End { get; set; }
		}
	}
}