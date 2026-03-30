using CTV.SafetyNet.Service_poc.Services;
using System.Text;

namespace CTV.SafetyNet.Service_poc;

public partial class MainForm : Form
{
	private readonly VideoAnalyzer _videoAnalyzer;
	private readonly Panel _dropPanel;
	private readonly Label _dropLabel;
	private readonly Label _statusLabel;
	private readonly Button _closeButton;

	private readonly DataGridView _resultsGrid;
	private readonly Button _saveButton;
	private readonly Panel _bottomPanel;
	private readonly GroupBox _resultsGroupBox;

	private byte[]? _lastVideoBytes;
	private string? _lastSuggestedFileName;

	private static readonly string[] AllowedExtensions =
	{
		".mp4", ".mov", ".avi", ".mkv", ".wmv", ".webm", ".m4v"
	};

	public MainForm(VideoAnalyzer videoAnalyzers)
	{
		_videoAnalyzer = videoAnalyzers;

		Text = "Video QA";
		StartPosition = FormStartPosition.CenterScreen;
		Size = new Size(1000, 720);
		MinimumSize = new Size(900, 620);
		BackColor = Color.White;

		_dropPanel = new Panel
		{
			AllowDrop = true,
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = Color.FromArgb(245, 247, 250),
			Location = new Point(24, 24),
			Size = new Size(936, 180),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};

		_dropLabel = new Label
		{
			Text = "Drop a video file here",
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleCenter,
			Font = new Font("Segoe UI", 18, FontStyle.Regular),
			Dock = DockStyle.Fill
		};

		_statusLabel = new Label
		{
			Text = "Waiting for a video...",
			AutoSize = false,
			Location = new Point(24, 216),
			Size = new Size(936, 28),
			Font = new Font("Segoe UI", 10, FontStyle.Regular),
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
		};

		_resultsGroupBox = new GroupBox
		{
			Text = "Results",
			Location = new Point(24, 255),
			Size = new Size(936, 360),
			Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
		};

		_resultsGrid = new DataGridView
		{
			Location = new Point(12, 28),
			Size = new Size(912, 320),
			Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
			ReadOnly = true,
			AllowUserToAddRows = false,
			AllowUserToDeleteRows = false,
			AllowUserToResizeRows = false,
			RowHeadersVisible = false,
			MultiSelect = false,
			SelectionMode = DataGridViewSelectionMode.FullRowSelect,
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			BackgroundColor = Color.White,
			BorderStyle = BorderStyle.None
		};

		_resultsGrid.Columns.Add("CheckName", "Check");
		_resultsGrid.Columns.Add("Passed", "Passed");
		_resultsGrid.Columns.Add("FixAttempted", "Fix Attempted");
		_resultsGrid.Columns.Add("FixSucceeded", "Fix Succeeded");
		_resultsGrid.Columns.Add("Message", "Message");

		_resultsGrid.Columns["CheckName"]!.FillWeight = 20;
		_resultsGrid.Columns["Passed"]!.FillWeight = 10;
		_resultsGrid.Columns["FixAttempted"]!.FillWeight = 15;
		_resultsGrid.Columns["FixSucceeded"]!.FillWeight = 15;
		_resultsGrid.Columns["Message"]!.FillWeight = 40;

		_bottomPanel = new Panel
		{
			Location = new Point(24, 628),
			Size = new Size(936, 50),
			Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
		};

		_saveButton = new Button
		{
			Text = "Download Fixed Video",
			Size = new Size(190, 38),
			Location = new Point(0, 6),
			Enabled = false
		};

		_closeButton = new Button
		{
			Text = "Close",
			Size = new Size(120, 38),
			Location = new Point(_bottomPanel.Width - 120, 6),
			Anchor = AnchorStyles.Top | AnchorStyles.Right
		};

		_dropPanel.Controls.Add(_dropLabel);
		_resultsGroupBox.Controls.Add(_resultsGrid);
		_bottomPanel.Controls.Add(_saveButton);
		_bottomPanel.Controls.Add(_closeButton);

		Controls.Add(_dropPanel);
		Controls.Add(_statusLabel);
		Controls.Add(_resultsGroupBox);
		Controls.Add(_bottomPanel);

		_dropPanel.DragEnter += DropPanel_DragEnter;
		_dropPanel.DragDrop += DropPanel_DragDrop;
		_dropPanel.DragLeave += DropPanel_DragLeave;

		_closeButton.Click += (_, _) => Close();
		_saveButton.Click += SaveButton_Click;
	}

	private void AddResultRow(string checkName, bool passed, bool fixAttempted, bool fixSucceeded, string message)
	{
		int rowIndex = _resultsGrid.Rows.Add(
			checkName,
			passed ? "Yes" : "No",
			fixAttempted ? "Yes" : "No",
			fixSucceeded ? "Yes" : "No",
			message);

		var row = _resultsGrid.Rows[rowIndex];

		if (passed)
		{
			row.DefaultCellStyle.BackColor = Color.FromArgb(235, 248, 235);
		}
		else if (fixSucceeded)
		{
			row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
		}
		else
		{
			row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
		}
	}

	private void DropPanel_DragEnter(object? sender, DragEventArgs e)
	{
		if (TryGetSingleVideoFile(e.Data, out _))
		{
			e.Effect = DragDropEffects.Copy;
			_dropPanel.BackColor = Color.FromArgb(220, 240, 255);
			_dropLabel.Text = "Release to analyze video";
		}
		else
		{
			e.Effect = DragDropEffects.None;
			_dropPanel.BackColor = Color.FromArgb(255, 235, 235);
			_dropLabel.Text = "Only one video file is allowed";
		}
	}

	private async void DropPanel_DragDrop(object? sender, DragEventArgs e)
	{
		_dropPanel.BackColor = Color.FromArgb(245, 247, 250);

		if (!TryGetSingleVideoFile(e.Data, out var filePath))
		{
			_dropLabel.Text = "Drop a video file here";
			_statusLabel.Text = "Invalid file. Please drop exactly one video file.";
			return;
		}

		_dropLabel.Text = Path.GetFileName(filePath);
		_statusLabel.Text = "Running checks...";
		_dropPanel.Enabled = false;
		_saveButton.Enabled = false;
		_closeButton.Enabled = false;

		try
		{
			await RunChecksAsync(filePath);
		}
		catch (Exception ex)
		{
			_statusLabel.Text = $"Error: {ex.Message}";
		}
		finally
		{
			if (string.IsNullOrWhiteSpace(_dropLabel.Text))
			{
				_dropLabel.Text = "Drop a video file here";
			}

			_dropPanel.Enabled = true;
			_closeButton.Enabled = true;
		}
	}

	private async void SaveButton_Click(object? sender, EventArgs e)
	{
		if (_lastVideoBytes == null || _lastVideoBytes.Length == 0)
		{
			MessageBox.Show("No processed video available.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		using var dialog = new SaveFileDialog
		{
			Filter = "MP4 Video|*.mp4|All files|*.*",
			FileName = _lastSuggestedFileName ?? "processed_video.mp4"
		};

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		await File.WriteAllBytesAsync(dialog.FileName, _lastVideoBytes);
		MessageBox.Show("Video saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void DropPanel_DragLeave(object? sender, EventArgs e)
	{
		_dropPanel.BackColor = Color.FromArgb(245, 247, 250);
		_dropLabel.Text = "Drop a video file here";
	}

	private static bool TryGetSingleVideoFile(IDataObject? data, out string filePath)
	{
		filePath = string.Empty;

		if (data == null || !data.GetDataPresent(DataFormats.FileDrop))
			return false;

		var files = (string[]?)data.GetData(DataFormats.FileDrop);
		if (files == null || files.Length != 1)
			return false;

		var candidate = files[0];

		if (!File.Exists(candidate))
			return false;

		var extension = Path.GetExtension(candidate);
		if (string.IsNullOrWhiteSpace(extension))
			return false;

		if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
			return false;

		filePath = candidate;
		return true;
	}

	private async Task RunChecksAsync(string filePath)
	{
		_resultsGrid.Rows.Clear();
		_saveButton.Enabled = false;
		_lastVideoBytes = null;
		_lastSuggestedFileName = null;

		var result = await _videoAnalyzer.AnalyzeAsync(filePath);

		_lastVideoBytes = result.FinalVideoBytes;
		_lastSuggestedFileName = Path.GetFileNameWithoutExtension(filePath) + "_fixed.mp4";

		foreach (var issue in result.Issues)
		{
			var patch = result.Patches
				.LastOrDefault(p => string.Equals(
					p.CheckName,
					issue.CheckName.Replace(" (recheck)", ""),
					StringComparison.OrdinalIgnoreCase));

			AddResultRow(
				issue.CheckName,
				issue.Passed,
				patch?.Attempted == true,
				patch?.Succeeded == true,
				issue.Message);
		}

		if (result.FinalVideoBytes != null && result.FinalVideoBytes.Length > 0)
		{
			_saveButton.Enabled = true;
		}

		var failedCount = result.Issues.Count(i => !i.Passed && !i.CheckName.Contains("(recheck)"));
		_statusLabel.Text = failedCount == 0
			? $"Video accepted: {Path.GetFileName(filePath)}"
			: $"Analysis completed. Issues found: {failedCount}";
	}
}