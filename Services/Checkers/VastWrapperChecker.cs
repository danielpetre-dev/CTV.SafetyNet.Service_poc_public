using CTV.SafetyNet.Service_poc.Models;
using CTV.SafetyNet.Service_poc.Models.VideoLogging;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using System.Xml.Linq;

namespace CTV.SafetyNet.Service_poc.Services.Analyzers
{
	public class VastWrapperChecker : IVideoChecker
	{
		public string Name => "VAST Wrappers";
		public bool CanFix => false;
		public int Order => 200;

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

			using var http = new HttpClient();
			string currentUrl = status.VastUrl;
			int wrappers = 0;
			var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			while (true)
			{
				if (!visited.Add(currentUrl))
				{
					return new VideoIssue
					{
						CheckName = Name,
						Passed = false,
						Message = "Wrapper loop detected."
					};
				}

				var xml = await http.GetStringAsync(currentUrl, ct);
				var doc = XDocument.Parse(xml);

				var wrapperUri = doc.Descendants()
					.FirstOrDefault(x => x.Name.LocalName == "VASTAdTagURI")
					?.Value
					?.Trim();

				var mediaUrls = doc.Descendants()
					.Where(x => x.Name.LocalName == "MediaFile")
					.Select(x => x.Value.Trim())
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToList();

				if (mediaUrls.Any(u => !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
				{
					return new VideoIssue
					{
						CheckName = Name,
						Passed = false,
						Message = "Non-HTTPS MediaFile URL detected."
					};
				}

				if (string.IsNullOrWhiteSpace(wrapperUri))
					break;

				wrappers++;
				if (wrappers > 2)
				{
					return new VideoIssue
					{
						CheckName = Name,
						Passed = false,
						Message = $"Wrapper depth exceeded. Found {wrappers} wrappers."
					};
				}

				currentUrl = wrapperUri;
			}

			return new VideoIssue
			{
				CheckName = Name,
				Passed = true,
				Message = $"VAST wrappers OK. Wrapper depth={wrappers}"
			};
		}

		public Task<VideoPatch> FixAsync(VideoStatus status, CancellationToken ct = default)
		{
			return Task.FromResult(new VideoPatch
			{
				CheckName = Name,
				Attempted = false,
				Succeeded = false,
				Message = "VAST wrapper issues must be fixed upstream."
			});
		}
	}
}