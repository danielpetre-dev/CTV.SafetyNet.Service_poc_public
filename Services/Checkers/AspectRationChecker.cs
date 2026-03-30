using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class AspectRatioChecker : IVideoChecker
	{
		private readonly FfprobeService _ffprobeService;
		private readonly FfmpegService _ffmpegService;

		public AspectRatioChecker(
			FfprobeService ffprobeService,
			FfmpegService ffmpegService)
		{
			_ffprobeService = ffprobeService;
			_ffmpegService = ffmpegService;
		}

		public string Name => "Aspect Ratio";
		public bool CanFix => true;
		public int Order => 10;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var probe = await _ffprobeService.ProbeAsync(status.CurrentFilePath, ct);
			var video = probe.Streams.FirstOrDefault(s => s.CodecType == "video");

			if (video == null)
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "No video stream found."
				};
			}

			int width = video.Width ?? 0;
			int height = video.Height ?? 0;

			if (height == 0)
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "Invalid video height."
				};
			}

			double ratio = width / (double)height;
			double target = 16.0 / 9.0;
			double tolerance = 0.01;

			bool passed = Math.Abs(ratio - target) <= tolerance;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? $"Aspect ratio OK: {width}x{height}"
					: $"Invalid aspect ratio: {width}x{height}",
				Details = new Dictionary<string, object>
				{
					["Width"] = width,
					["Height"] = height,
					["Ratio"] = ratio
				}
			};
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var inputPath = status.CurrentFilePath;

			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_aspect_fixed.mp4");

			var args =
				$"-y -i \"{inputPath}\" " +
				"-vf \"scale=1920:1080:force_original_aspect_ratio=decrease," +
				"pad=1920:1080:(ow-iw)/2:(oh-ih)/2\" " +
				"-c:v libx264 -c:a aac -movflags +faststart " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = result.Success,
				Message = result.Success
					? "Video scaled/padded to 1920x1080 (16:9)."
					: $"Aspect ratio fix failed: {result.StdErr}",
				InputFilePath = inputPath,
				OutputFilePath = result.Success ? outputPath : null
			};
		}
	}
}