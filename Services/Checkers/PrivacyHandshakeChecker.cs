using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using System.Xml.Linq;

namespace CTV.SafetyNet.Service_poc.Services.Analyzers
{
	public class PrivacyHandshakeChecker : IVideoChecker
	{
		public string Name => "Privacy Handshake";
		public bool CanFix => false;
		public int Order => 210;

		public async Task<VideoIssue> CheckAsync(VideoStatus status, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(status.VastUrl))
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = true,
					Message = "No VAST URL provided. Check skipped."
				};
			}

			var url = status.VastUrl;

			if (!url.Contains("gdpr=", StringComparison.OrdinalIgnoreCase) ||
				!url.Contains("gdpr_consent=", StringComparison.OrdinalIgnoreCase))
			{
				return new VideoIssue
				{
					CheckName = Name,
					Passed = false,
					Message = "VAST request does not contain gdpr/gdpr_consent parameters."
				};
			}

			using var http = new HttpClient();
			var xml = await http.GetStringAsync(url, ct);

			bool looksLikeVast = xml.Contains("<VAST", StringComparison.OrdinalIgnoreCase);

			return new VideoIssue
			{
				CheckName = Name,
				Passed = looksLikeVast,
				Message = looksLikeVast
					? "Privacy handshake request structure looks valid."
					: "Response does not look like VAST XML."
			};
		}

		public Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			return Task.FromResult(new VideoPatch
			{
				CheckName = Name,
				Attempted = false,
				Succeeded = false,
				Message = "Privacy-handshake issues must be fixed in the ad request/bidder integration."
			});
		}
	}
}