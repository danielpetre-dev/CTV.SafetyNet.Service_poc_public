using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;

namespace CTV.SafetyNet.Service_poc.Services.Checkers
{
	public interface IVideoChecker
	{
		string Name { get; }
		bool CanFix { get; }

		Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default);
		Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default);
	}
}
