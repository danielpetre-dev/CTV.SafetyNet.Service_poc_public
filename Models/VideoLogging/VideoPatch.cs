namespace CTV.SafetyNet.Service_poc.Models.VideoLogging
{
	public class VideoPatch
	{
		public string CheckName { get; set; } = string.Empty;
		public bool Attempted { get; set; }
		public bool Succeeded { get; set; }
		public string Message { get; set; } = string.Empty;

		public string? InputFilePath { get; set; }
		public string? OutputFilePath { get; set; }
	}
}
