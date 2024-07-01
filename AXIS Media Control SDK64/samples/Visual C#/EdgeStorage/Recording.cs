using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EdgeStorage
{
	class Recording
	{
		public string RecordingId { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime StopTime { get; set; }

		public string RecordingStatus { get; set; }

		public string MimeType { get; set; }

		public string FrameRate { get; set; }

		public string Audio { get; set; }

		[System.ComponentModel.Browsable(false)]
		public string Host { get; set; }

		[System.ComponentModel.Browsable(false)]
		public System.Net.NetworkCredential Credentials { get; set; }
	}
}
