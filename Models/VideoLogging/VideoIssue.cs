namespace CTV.SafetyNet.Service_poc.Models.VideoLogging
{
	public class VideoIssue
	{
		public string CheckName { get; set; } = string.Empty;
		public bool Passed { get; set; }
		public string Message { get; set; } = string.Empty;

		public Dictionary<string, object>? Details { get; set; }
	}
}
