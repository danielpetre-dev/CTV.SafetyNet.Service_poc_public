namespace CTV.SafetyNet.Service_poc.Helpers
{
	public static class VideoTempFileHelper
	{
		public static async Task<string> SaveBytesToTempMp4Async(byte[] videoBytes, CancellationToken ct = default)
		{
			var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mp4");
			await File.WriteAllBytesAsync(path, videoBytes, ct);
			return path;
		}
	}
}
