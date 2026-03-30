using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class FileValidationChecker : IVideoChecker
	{
		private readonly FfprobeService _ffprobeService;
		private readonly FfmpegService _ffmpegService;
		private readonly long _minBitrate = 15_000_000;
		private readonly long _maxBitrate = 30_000_000;

		public FileValidationChecker(FfprobeService ffprobeService, FfmpegService ffmpegService)
		{
			_ffprobeService = ffprobeService;
			_ffmpegService = ffmpegService;
		}

		public string Name => "File Validation";
		public bool CanFix => true;

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

			var formatName = probe.Format?.FormatName ?? string.Empty;
			bool containerOk = formatName.Contains("mp4", StringComparison.OrdinalIgnoreCase)
							   || formatName.Contains("mov", StringComparison.OrdinalIgnoreCase);

			bool resolutionOk = video.Width == 1920 && video.Height == 1080;

			long bitrate = FfprobeService.ParseLong(video.BitRate ?? probe.Format?.BitRate);
			bool bitrateOk = bitrate >= _minBitrate && bitrate <= _maxBitrate;

			bool passed = containerOk && resolutionOk && bitrateOk;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? "Container, resolution, and bitrate are valid."
					: $"Validation failed. ContainerOk={containerOk}, ResolutionOk={resolutionOk}, BitrateOk={bitrateOk}",
				Details = new Dictionary<string, object>
				{
					["FormatName"] = formatName,
					["Width"] = video.Width ?? 0,
					["Height"] = video.Height ?? 0,
					["Bitrate"] = bitrate,
					["MinBitrate"] = _minBitrate,
					["MaxBitrate"] = _maxBitrate
				}
			};
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var inputPath = status.CurrentFilePath;
			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_validated.mp4");

			// choose a sane midpoint target
			long targetBitrate = Math.Min(Math.Max(_minBitrate, 20_000_000), _maxBitrate);

			var args =
				$"-y -i \"{inputPath}\" " +
				"-vf \"scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2\" " +
				$"-c:v libx264 -profile:v high -level 4.1 -b:v {targetBitrate} -maxrate {targetBitrate} -bufsize {targetBitrate * 2} " +
				"-c:a aac -b:a 192k -ar 48000 -movflags +faststart " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = result.Success,
				Message = result.Success
					? "File transcoded to CTV-ready MP4."
					: $"Validation fix failed: {result.StdErr}",
				InputFilePath = inputPath,
				OutputFilePath = result.Success ? outputPath : null
			};
		}
	}
}