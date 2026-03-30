using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Services;

namespace CTV.SafetyNet.Service_poc.Helpers
{
	public static class VideoGeneralInfoExtractor
	{
		public static async Task<VideoGeneralInfo> ExtractAsync(string filePath, CancellationToken ct = default)
		{
			var probeService = new FfprobeService();

			var probe = await probeService.ProbeAsync(filePath, ct);

			var video = probe.Streams.FirstOrDefault(s => s.CodecType == "video");
			var audio = probe.Streams.FirstOrDefault(s => s.CodecType == "audio");

			long.TryParse(probe.Format?.Size, out var size);
			long.TryParse(video?.BitRate ?? probe.Format?.BitRate, out var bitrate);
			long.TryParse(video?.NbFrames, out var frameCount);

			double.TryParse(
				video?.Duration ?? probe.Format?.Duration,
				System.Globalization.CultureInfo.InvariantCulture,
				out var duration);

			var fps = FfprobeService.ParseFraction(video?.AvgFrameRate);

			int width = video?.Width ?? 0;
			int height = video?.Height ?? 0;

			return new VideoGeneralInfo
			{
				FileSizeBytes = size > 0 ? size : new FileInfo(filePath).Length,
				Container = probe.Format?.FormatName ?? string.Empty,
				VideoCodec = video?.CodecName,
				AudioCodec = audio?.CodecName,
				Width = width,
				Height = height,
				AspectRatio = height == 0 ? 0 : width / (double)height,
				DurationSeconds = duration,
				FrameRate = fps,
				FrameCount = frameCount > 0 ? frameCount : null,
				Bitrate = bitrate > 0 ? bitrate : null,
				HasAudio = audio != null
			};
		}
	}
}
