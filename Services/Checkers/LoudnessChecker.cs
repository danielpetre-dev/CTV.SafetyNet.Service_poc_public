using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public class LoudnessChecker : IVideoChecker
	{
		private readonly FfmpegService _ffmpegService;
		private readonly FfprobeService _ffprobeService;
		private readonly string _region;

		public LoudnessChecker(FfmpegService ffmpegService, FfprobeService ffprobeService, string region)
		{
			_ffmpegService = ffmpegService;
			_ffprobeService = ffprobeService;
			_region = region;
		}

		public string Name => "Loudness";
		public bool CanFix => true;
		public int Order => 40;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var probe = await _ffprobeService.ProbeAsync(status.CurrentFilePath, ct);
			var audioStream = probe.Streams.FirstOrDefault(s =>
				string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase));

			if (audioStream == null)
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "No audio stream found. Loudness cannot be measured.",
					Details = new Dictionary<string, object>
					{
						["Region"] = _region,
						["HasAudio"] = false
					}
				};
			}

			double target = GetTargetLoudness();
			double tolerance = 0.5;

			var measured = await MeasureIntegratedLoudnessAsync(status.CurrentFilePath, target, ct);
			
			bool passed = measured != 0 && Math.Abs(measured - target) <= tolerance;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? $"Loudness OK. Measured={measured:F2}, Target={target:F2}"
					: $"Loudness invalid. Measured={measured:F2}, Target={target:F2}",
				Details = new Dictionary<string, object>
				{
					["MeasuredIntegratedLoudness"] = measured,
					["TargetLoudness"] = target,
					["Tolerance"] = tolerance,
					["Region"] = _region,
					["HasAudio"] = true
				}
			};
		}

		public async Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			var probe = await _ffprobeService.ProbeAsync(status.CurrentFilePath, ct);
			var audioStream = probe.Streams.FirstOrDefault(s =>
				string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase));

			if (audioStream == null)
			{
				return new VideoPatch
				{
					CheckName = Name,
					Attempted = false,
					Succeeded = false,
					Message = "No audio stream found. Loudness fix cannot be applied.",
					InputFilePath = status.CurrentFilePath
				};
			}

			var inputPath = status.CurrentFilePath;
			double target = GetTargetLoudness();

			var outputPath = Path.Combine(
				Path.GetTempPath(),
				$"{Guid.NewGuid():N}_loudness_fixed.mp4");

			var args =
				$"-y -i \"{inputPath}\" " +
				$"-af loudnorm=I={target.ToString(CultureInfo.InvariantCulture)}:LRA=7:TP=-2 " +
				"-c:v copy -c:a aac -b:a 192k " +
				$"\"{outputPath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);

			return new VideoPatch
			{
				CheckName = Name,
				Attempted = true,
				Succeeded = result.Success,
				Message = result.Success
					? $"Loudness normalization applied to target {target:F1}."
					: $"Loudness fix failed: {result.StdErr}",
				InputFilePath = inputPath,
				OutputFilePath = result.Success ? outputPath : null
			};
		}

		private double GetTargetLoudness()
		{
			return _region.Equals("US", StringComparison.OrdinalIgnoreCase) ? -24.0 : -23.0;
		}

		private async Task<double> MeasureIntegratedLoudnessAsync(string filePath, double target, CancellationToken ct)
		{
			var args =
				$"-i \"{filePath}\" -af loudnorm=I={target.ToString(CultureInfo.InvariantCulture)}:LRA=7:TP=-2:print_format=json -f null -";

			var result = await _ffmpegService.RunAsync(args, ct);
			var stderr = result.StdErr;

			var match = Regex.Match(
				stderr,
				@"""input_i""\s*:\s*""(?<v>-?\d+(\.\d+)?)""",
				RegexOptions.IgnoreCase | RegexOptions.Multiline);

			if (!match.Success)
			{
				return 0;
			}

			return double.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture);
		}
	}
}