using System.Globalization;
using System.Text.Json;
using CTV.SafetyNet.Service_poc.Models;

namespace CTV.SafetyNet.Service_poc.Services
{
	public class FfprobeService
	{
		private readonly string _ffprobePath;

		public FfprobeService(string ffprobePath = "ffprobe")
		{
			_ffprobePath = ffprobePath;
		}

		public async Task<FfprobeResponse> ProbeAsync(string filePath, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

			if (!File.Exists(filePath))
				throw new FileNotFoundException("Video file not found.", filePath);

			var arguments =
				$"-v error -print_format json -show_format -show_streams \"{filePath}\"";

			var result = await ProcessRunner.RunAsync(_ffprobePath, arguments, ct);

			if (!result.Success)
			{
				throw new InvalidOperationException(
					$"ffprobe failed for '{filePath}'. ExitCode={result.ExitCode}. Error={result.StdErr}");
			}

			var probe = JsonSerializer.Deserialize<FfprobeResponse>(
				result.StdOut,
				new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

			if (probe == null)
				throw new InvalidOperationException("Could not deserialize ffprobe output.");

			return probe;
		}

		public static double ParseFraction(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return 0;

			var parts = value.Split('/');
			if (parts.Length == 2 &&
				double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var numerator) &&
				double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var denominator) &&
				denominator != 0)
			{
				return numerator / denominator;
			}

			return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
				? number
				: 0;
		}

		public static double ParseDouble(string? value)
		{
			return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
				? number
				: 0;
		}

		public static long ParseLong(string? value)
		{
			return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
				? number
				: 0;
		}
	}
}