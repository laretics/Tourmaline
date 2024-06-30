using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EdgeStorage
{
	public partial class RecordingListForm : Form
	{
		Version firmwareVersion = new Version();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new RecordingListForm());
		}

		public RecordingListForm()
		{
			InitializeComponent();
		}

		private void connectButton_Click(object sender, EventArgs e)
		{
			dataGridView1.DataSource = null;
			playButton.Enabled = exportButton.Enabled = false;

			DownloadRecordingList();
			GetFirmwareVersion();
		}

		private void GetFirmwareVersion()
		{
			try
			{
				UriBuilder uriBuilder = new UriBuilder();
				uriBuilder.Scheme = "http://";
				uriBuilder.Host = ipBox.Text;
				uriBuilder.Path = "/axis-cgi/param.cgi";
				uriBuilder.Query = "action=list&group=root.Properties.Firmware.Version";

				// <httpWebRequest useUnsafeHeaderParsing="true" /> in app.config
				WebClient webClient = new WebClient();
				if (!String.IsNullOrEmpty(userNameBox.Text))
				{
					webClient.Credentials = new NetworkCredential(userNameBox.Text, passwordBox.Text);
				}
				string result = webClient.DownloadString(uriBuilder.ToString());
				Match match = Regex.Match(result, @"[^=]+=(.+)");
				try
				{
					firmwareVersion = new Version(match.Groups[1].Value);
					firmwareLabel.Text = "Firmware version: " + firmwareVersion.ToString(2);
				}
				catch
				{
					firmwareLabel.Text = "Firmware version: " + match.Groups[1].Value;
				}
			}
			catch
			{
				firmwareLabel.Text = "Firmware version: N/A";
			}
		}

		private void DownloadRecordingList()
		{
			try
			{
				// Comprehensive information about the Edge Storage API can be found on the partner pages:
				// http://www.axis.com/partner_pages/vapix3.php
				UriBuilder uriBuilder = new UriBuilder();
				uriBuilder.Scheme = "http://";
				uriBuilder.Host = ipBox.Text;
				uriBuilder.Path = "/axis-cgi/record/list.cgi";
				uriBuilder.Query = "recordingid=all";

				WebClient webClient = new WebClient();
				if (!String.IsNullOrEmpty(userNameBox.Text))
				{
					webClient.Credentials = new NetworkCredential(userNameBox.Text, passwordBox.Text);
				}
				string result = webClient.DownloadString(uriBuilder.ToString());

				XDocument xmlDoc = XDocument.Load(new System.IO.StringReader(result));
				var query = from r in xmlDoc.Descendants("recording")
										select new Recording
										{
											RecordingId = (string)r.Attribute("recordingid").Value,
											StartTime = IntParseDateTime(r.Attribute("starttime").Value),
											StopTime = IntParseDateTime(r.Attribute("stoptime").Value),
											RecordingStatus = (string)r.Attribute("recordingstatus").Value,
											MimeType = r.Descendants("video").Count() > 0 ?
												(string)r.Descendants("video").FirstOrDefault().Attribute("mimetype").Value : "",
											FrameRate = r.Descendants("video").Count() > 0 ?
												(string)r.Descendants("video").FirstOrDefault().Attribute("framerate").Value : "",
											Audio = (string)((r.Descendants("audio").Count() > 0) ? "yes" : "no"),
											Host = ipBox.Text,
											Credentials = new NetworkCredential(userNameBox.Text, passwordBox.Text)
										};

				dataGridView1.DataSource = query.ToList();

				if (query.Count() > 0)
				{
					playButton.Enabled = exportButton.Enabled = true;
				}
				else
				{
					MessageBox.Show("No recordings are available on " + ipBox.Text);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("Failed to download recording list. " + ex.Message +
					"\n\n(Note that the Edge Storage API is supported from firmware 5.40)");
			}
		}

		private DateTime IntParseDateTime(string dateTimeString)
		{
			DateTime parsedDateTime;
			if (DateTime.TryParse(dateTimeString, out parsedDateTime))
			{
				return parsedDateTime;
			}
			else
			{
				return new DateTime();
			}
		}

		private void playButton_Click(object sender, EventArgs e)
		{
			if (dataGridView1.SelectedRows.Count > 0)
			{
				Recording recording = (Recording)dataGridView1.SelectedRows[0].DataBoundItem;

				UriBuilder uriBuilder = new UriBuilder();
				uriBuilder.Scheme = "axrtsphttp://";
				uriBuilder.Host = recording.Host;
				uriBuilder.Path = "/axis-media/media.amp";
				uriBuilder.Query = "recordingid=" + recording.RecordingId;

				PlaybackWindow playbackWindow = new PlaybackWindow(
					uriBuilder.ToString(),
					recording.Credentials.UserName,
					recording.Credentials.Password,
					firmwareVersion);

				if (recording.RecordingStatus == "recording")
				{
					// explicitly set currently known duration for ongoing recordings
					TimeSpan currentDuration = (recording.StopTime - recording.StartTime);
					if (currentDuration.Ticks > 0)
					{
						playbackWindow.MediaDuration = (ulong)currentDuration.Ticks / 10000; // milliseconds
					}
				}

				playbackWindow.ShowDialog();
			}
		}

		private void dataGridView1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			playButton.PerformClick();
		}

		private void exportButton_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Export, e.g. save to ASF-file, could be implemented " +
			"using the AXIS Media Parser SDK (available from the Application Development Program).");
		}
	}
}
