using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Models
{
	public class VideoStatus
	{
		public string OriginalFilePath { get; set; } = string.Empty;
		public string CurrentFilePath { get; set; } = string.Empty;

		public VideoGeneralInfo GeneralInfo { get; set; } = new();

		public List<VideoIssue> Issues { get; set; } = new();
		public List<VideoPatch> Patches { get; set; } = new();

		public Dictionary<string, object> Items { get; set; } = new();

		public string? VastUrl { get; set; }

		public string? ConsentString { get; set; }

		public bool GdprApplies { get; set; }
	}
}
