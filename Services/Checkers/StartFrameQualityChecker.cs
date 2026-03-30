using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using OpenCvSharp;

namespace CTV.SafetyNet.Service_poc.Services.Analyzers
{
	public class StartFrameQualityChecker : IVideoChecker
	{
		private readonly FfmpegService _ffmpegService;

		public StartFrameQualityChecker(FfmpegService ffmpegService)
		{
			_ffmpegService = ffmpegService;
		}

		public string Name => "Start Frame Quality";
		public bool CanFix => false;
		public int Order => 80;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var framePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_firstframe.jpg");

			var args =
				$"-y -ss 0.02 -i \"{status.CurrentFilePath}\" -frames:v 1 \"{framePath}\"";

			var result = await _ffmpegService.RunAsync(args, ct);
			if (!result.Success || !File.Exists(framePath))
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "Could not extract first frame."
				};
			}

			using var image = Cv2.ImRead(framePath, ImreadModes.Color);
			using var gray = new Mat();
			Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

			double brightness = Cv2.Mean(gray).Val0;

			using var lap = new Mat();
			Cv2.Laplacian(gray, lap, MatType.CV_64F);

			using var mean = new Mat();
			using var stdDev = new Mat();
			Cv2.MeanStdDev(lap, mean, stdDev);

			double blurScore = Math.Pow(stdDev.At<double>(0), 2);

			bool dark = brightness < 20;
			bool blurry = blurScore < 50;
			bool passed = !dark && !blurry;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? $"Start frame quality OK. Brightness={brightness:F2}, BlurScore={blurScore:F2}"
					: $"Start frame quality failed. Brightness={brightness:F2}, BlurScore={blurScore:F2}",
				Details = new Dictionary<string, object>
				{
					["Brightness"] = brightness,
					["BlurScore"] = blurScore
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
				Message = "Start-frame quality is not safely auto-fixable.",
				InputFilePath = status.CurrentFilePath
			});
		}
	}
}