namespace CTV.SafetyNet.Service_poc.Services
{
	public class FfmpegService
	{
		private string _ffmpegPath;

		public FfmpegService(string path) { 
			_ffmpegPath = path;
		}

		public Task<ProcessResult> RunAsync(string arguments, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(_ffmpegPath))
				throw new ArgumentException("ffmpeg path is required.", nameof(_ffmpegPath));

			if (!File.Exists(_ffmpegPath))
				throw new FileNotFoundException("ffmpeg executable not found.", _ffmpegPath);

			return ProcessRunner.RunAsync(_ffmpegPath, arguments, ct);
		}
	}
}