using CTV.SafetyNet.Service_poc.Services;
using CTV.SafetyNet.Service_poc.Services.Analyzers;
using CTV.SafetyNet.Service_poc.Services.Checkers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CTV.SafetyNet.Service_poc
{
    public static class Program
    {
		[STAThread]
		static void Main()
		{
			ApplicationConfiguration.Initialize();

			var host = CreateHostBuilder().Build();

			var mainForm = host.Services.GetRequiredService<MainForm>();
			Application.Run(mainForm);
		}

		static IHostBuilder CreateHostBuilder()
		{
			return Host.CreateDefaultBuilder()
			.ConfigureServices((context, services) =>
			{
				var baseDir = AppDomain.CurrentDomain.BaseDirectory;
				var ffprobePath = Path.Combine(baseDir, "Tools", "ffprobe.exe");
				var ffmpegPath = Path.Combine(baseDir, "Tools", "ffmpeg.exe");

				// Core services
				services.AddSingleton(new FfprobeService(ffprobePath));
				services.AddSingleton(new FfmpegService(ffmpegPath));

				// Checkers
				services.AddSingleton<IVideoChecker>(sp =>
					new FileSizeChecker(
						60 * 1024 * 1024,
						sp.GetRequiredService<FfprobeService>(),
						sp.GetRequiredService<FfmpegService>()));

				services.AddSingleton<IVideoChecker>(sp =>
					new DurationChecker(
						sp.GetRequiredService<FfprobeService>(),
						sp.GetRequiredService<FfmpegService>(),
						15));

				services.AddSingleton<IVideoChecker>(sp =>
					new AspectRatioChecker(
						sp.GetRequiredService<FfprobeService>(),
						sp.GetRequiredService<FfmpegService>()));

				services.AddSingleton<IVideoChecker>(sp =>
					new BlackEdgeChecker(
						sp.GetRequiredService<FfmpegService>(),
						sp.GetRequiredService<FfprobeService>()));

				services.AddSingleton<IVideoChecker>(sp =>
					new LoudnessChecker(
						sp.GetRequiredService<FfmpegService>(),
						sp.GetRequiredService<FfprobeService>(),
						"EU"));

				services.AddSingleton<IVideoChecker>(sp =>
					new FileValidationChecker(
						sp.GetRequiredService<FfprobeService>(),
						sp.GetRequiredService<FfmpegService>()));

				services.AddSingleton<IVideoChecker>(sp =>
					new FreezeFrameChecker(
						sp.GetRequiredService<FfmpegService>()));

				services.AddSingleton<IVideoChecker>(sp =>
					new SingleColorFrameChecker());

				services.AddSingleton<IVideoChecker>(sp =>
					new StartFrameQualityChecker(
						sp.GetRequiredService<FfmpegService>()));

				services.AddSingleton<IVideoChecker>(sp => new VastWrapperChecker());
					
				services.AddSingleton<IVideoChecker>(sp => new PrivacyHandshakeChecker());

				services.AddSingleton<VideoAnalyzer>();

				// Forms
				services.AddTransient<MainForm>();
			});
		}
	}
}