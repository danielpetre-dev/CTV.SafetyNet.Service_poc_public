using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using System.Globalization;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class DurationChecker : IVideoChecker
	{
		private readonly FfprobeService _ffprobeService;
		private readonly FfmpegService _ffmpegService;
		private readonly double _expectedSeconds;

		public DurationChecker(
			FfprobeService ffprobeService,
			FfmpegService ffmpegService,
			double expectedSeconds)
		{
			_ffprobeService = ffprobeService;
			_ffmpegService = ffmpegService;
			_expectedSeconds = expectedSeconds;
		}

		public string Name => "Duration";
		public bool CanFix => false;
		public int Order => 20;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var probe = await _ffprobeService.ProbeAsync(status.CurrentFilePath, ct);
			var video = probe.Streams.FirstOrDefault(s => s.CodecType == "video");

			double duration = FfprobeService.ParseDouble(video?.Duration ?? probe.Format?.Duration);
			double fps = FfprobeService.ParseFraction(video?.AvgFrameRate);

			// tolerance = one frame
			double tolerance = fps > 0 ? 1.0 / fps : 0.04;

			bool passed = Math.Abs(duration - _expectedSeconds) <= tolerance;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? $"Duration OK: {duration:F3}s"
					: $"Invalid duration: {duration:F3}s, expected {_expectedSeconds:F3}s",
				Details = new Dictionary<string, object>
				{
					["ActualDuration"] = duration,
					["ExpectedDuration"] = _expectedSeconds,
					["Tolerance"] = tolerance,
					["FPS"] = fps
				}
			};
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var inputPath = status.CurrentFilePath;

			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_duration_fixed.mp4");

			// Trim or pad video to exact duration
			var args =
				$"-y -i \"{inputPath}\" " +
				$"-t {_expectedSeconds.ToString(CultureInfo.InvariantCulture)} " +
				"-c:v libx264 -c:a aac -movflags +faststart " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = result.Success,
				Message = result.Success
					? $"Video trimmed/padded to {_expectedSeconds:F2}s"
					: $"Duration fix failed: {result.StdErr}",
				InputFilePath = inputPath,
				OutputFilePath = result.Success ? outputPath : null
			};
		}
	}
}