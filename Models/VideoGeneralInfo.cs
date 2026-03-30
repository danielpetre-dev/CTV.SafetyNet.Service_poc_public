namespace CTV.SafetyNet.Service_poc.Models
{
	public class VideoGeneralInfo
	{
		public long FileSizeBytes { get; set; }
		public double FileSizeMb => FileSizeBytes / 1024d / 1024d;

		public string Container { get; set; } = string.Empty;

		public string? VideoCodec { get; set; }
		public string? AudioCodec { get; set; }

		public int Width { get; set; }
		public int Height { get; set; }
		public double AspectRatio { get; set; }

		public double DurationSeconds { get; set; }
		public double FrameRate { get; set; }
		public long? FrameCount { get; set; }

		public long? Bitrate { get; set; }
		public bool HasAudio { get; set; }
	}
}
