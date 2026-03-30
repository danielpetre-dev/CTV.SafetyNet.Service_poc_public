using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using OpenCvSharp;
using System;

namespace CTV.SafetyNet.Service_poc.Services.Analyzers
{
	public class SingleColorFrameChecker : IVideoChecker
	{
		public string Name => "Single Color Frames";
		public bool CanFix => false;
		public int Order => 70;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			var suspicious = new List<double>();

			using var capture = new VideoCapture(status.CurrentFilePath);
			if (!capture.IsOpened())
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "Could not open video for frame analysis."
				};
			}

			double fps = capture.Fps;
			if (fps <= 0) fps = 25;

			int totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);
			int stepFrames = Math.Max(1, (int)Math.Round(fps * 0.2)); // every 200 ms

			using var frame = new Mat();
			using var gray = new Mat();
			using var mean = new Mat();
			using var stdDev = new Mat();

			for (int i = 0; i < totalFrames; i += stepFrames)
			{
				ct.ThrowIfCancellationRequested();

				capture.Set(VideoCaptureProperties.PosFrames, i);
				if (!capture.Read(frame) || frame.Empty())
					continue;

				Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
				Cv2.MeanStdDev(gray, mean, stdDev);

				double sigma = stdDev.At<double>(0);

				if (sigma < 3.0)
				{
					suspicious.Add(i / fps);
				}
			}

			bool passed = suspicious.Count == 0;

			return new VideoIssue
			{
				CheckName = Name,
				Passed = passed,
				Message = passed
					? "No single-color frames detected."
					: $"Single-color frames detected: {suspicious.Count}",
				Details = new Dictionary<string, object>
				{
					["Count"] = suspicious.Count,
					["Timestamps"] = string.Join(", ", suspicious.Select(x => x.ToString("F2")))
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
				Message = "Single-color frames are not safely auto-fixable.",
				InputFilePath = status.CurrentFilePath
			});
		}
	}
}