namespace CTV.SafetyNet.Service_poc.Models
{
	using System.Text.Json.Serialization;

	public sealed class FfprobeResponse
	{
		[JsonPropertyName("streams")]
		public List<FfprobeStream> Streams { get; set; } = new();

		[JsonPropertyName("format")]
		public FfprobeFormat? Format { get; set; }
	}

	public sealed class FfprobeStream
	{
		[JsonPropertyName("codec_type")]
		public string? CodecType { get; set; }

		[JsonPropertyName("codec_name")]
		public string? CodecName { get; set; }

		[JsonPropertyName("width")]
		public int? Width { get; set; }

		[JsonPropertyName("height")]
		public int? Height { get; set; }

		[JsonPropertyName("bit_rate")]
		public string? BitRate { get; set; }

		[JsonPropertyName("avg_frame_rate")]
		public string? AvgFrameRate { get; set; }

		[JsonPropertyName("duration")]
		public string? Duration { get; set; }

		[JsonPropertyName("nb_frames")]
		public string? NbFrames { get; set; }
	}

	public sealed class FfprobeFormat
	{
		[JsonPropertyName("filename")]
		public string? FileName { get; set; }

		[JsonPropertyName("format_name")]
		public string? FormatName { get; set; }

		[JsonPropertyName("duration")]
		public string? Duration { get; set; }

		[JsonPropertyName("size")]
		public string? Size { get; set; }

		[JsonPropertyName("bit_rate")]
		public string? BitRate { get; set; }
	}
}
