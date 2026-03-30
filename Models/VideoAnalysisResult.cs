using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Models
{
	public class VideoAnalysisResult
	{
		public string OriginalFilePath { get; set; } = string.Empty;
		public string FinalFilePath { get; set; } = string.Empty;

		public byte[]? FinalVideoBytes { get; set; }

		public VideoGeneralInfo GeneralInfo { get; set; } = new();

		public List<VideoIssue> Issues { get; set; } = new();
		public List<VideoPatch> Patches { get; set; } = new();

		public bool HasErrors => Issues.Any(i => !i.Passed);
	}
}
