using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class FileSizeChecker : IVideoChecker
	{
		private readonly long _maxBytes = 1000000;

		private readonly FfprobeService _ffprobeService;
		private readonly FfmpegService _ffmpegService;

		public FileSizeChecker(long size, FfprobeService ffprobeService, FfmpegService ffmpegService)
		{
			_maxBytes = size;
			_ffprobeService = ffprobeService;
			_ffmpegService = ffmpegService;
		}

		public string Name => "File Size";
		public bool CanFix => false;

		public Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			long actual = new FileInfo(status.CurrentFilePath).Length;

			return Task.FromResult(new VideoIssue
			{
				CheckName = Name,
				Passed = actual <= _maxBytes,
				Message = actual <= _maxBytes
					? $"File size OK: {actual} bytes"
					: $"File too large: {actual} bytes. Max allowed: {_maxBytes}",
				Details = new Dictionary<string, object>
				{
					["ActualBytes"] = actual,
					["MaxBytes"] = _maxBytes
				}
			});
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var inputPath = status.CurrentFilePath;


			var probe = await _ffprobeService.ProbeAsync(inputPath, ct);
			var video = probe.Streams.FirstOrDefault(s => s.CodecType == "video");
			var duration = FfprobeService.ParseDouble(video?.Duration ?? probe.Format?.Duration);

			if (duration <= 0)
			{
				return new VideoPatch
				{
					CheckName = Name,
					Attempted = true,
					Succeeded = false,
					Message = "Could not determine duration, so target bitrate could not be calculated.",
					InputFilePath = inputPath
				};
			}

			// total bitrate budget in bits/sec
			var totalTargetBitrate = (long)(_maxBytes * 8d / duration);

			// keep some room for container overhead and audio
			totalTargetBitrate = (long)(totalTargetBitrate * 0.92);

			// reserve audio bitrate
			const int audioBitrate = 192_000;
			var videoBitrate = totalTargetBitrate - audioBitrate;

			if (videoBitrate < 500_000)
			{
				return new VideoPatch
				{
					CheckName = Name,
					Attempted = true,
					Succeeded = false,
					Message = $"Calculated target video bitrate is too low: {videoBitrate} bps",
					InputFilePath = inputPath
				};
			}

			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_filesize_fixed.mp4");

			var args =
				$"-y -i \"{inputPath}\" " +
				$"-c:v libx264 -preset medium -b:v {videoBitrate} -maxrate {videoBitrate} -bufsize {videoBitrate * 2} " +
				$"-c:a aac -b:a {audioBitrate} -movflags +faststart " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			if (!result.Success)
			{
				return new VideoPatch
				{
					CheckName = Name,
					Attempted = true,
					Succeeded = false,
					Message = $"File size fix failed: {result.StdErr}",
					InputFilePath = inputPath
				};
			}

			var newSize = new FileInfo(outputPath).Length;
			var passed = newSize <= _maxBytes;

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = passed,
				Message = passed
					? $"File size reduced successfully. New size: {newSize} bytes"
					: $"Re-encode completed but file is still too large: {newSize} bytes",
				InputFilePath = inputPath,
				OutputFilePath = outputPath
			};
		}
	}
}
