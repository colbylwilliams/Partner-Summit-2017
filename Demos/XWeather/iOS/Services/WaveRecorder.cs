//
//  Mostly Copyright 2013, Sami M. Kallio, changes 2016 Nate Rickard
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
using System;
using System.IO;
using System.Text;

namespace XWeather.WeatherBot
{
	public class WaveRecorder : IDisposable
	{
		StreamWriter streamWriter;
		BinaryWriter writer;
		int byteCount;
		AudioStream stream;

		public bool StartRecorder (AudioStream stream, string fileName)
		{
			if (this.stream != null || stream == null) {
				return false;
			}

			this.stream = stream;

			try {
				this.streamWriter = new StreamWriter (fileName, false);
				this.writer = new BinaryWriter (this.streamWriter.BaseStream, Encoding.UTF8);
			} catch (Exception) {
				return false;
			}

			this.byteCount = 0;
			this.stream.OnBroadcast += OnStreamBroadcast;
			this.stream.OnActiveChanged += StreamActiveChanged;

			if (!this.stream.Active) {
				this.stream.Start ();
			}

			return true;
		}


		void StreamActiveChanged (object sender, bool active)
		{
			if (!active) {
				StopRecorder ();
			}
		}


		void OnStreamBroadcast (object sender, byte [] bytes)
		{
			if (this.streamWriter != null) {
				this.writer.Write (bytes);
				this.byteCount += bytes.Length;
			}
		}


		public void StopRecorder ()
		{
			if (this.stream != null) {
				this.stream.OnBroadcast -= OnStreamBroadcast;
				this.stream.OnActiveChanged -= StreamActiveChanged;
			}

			if (this.streamWriter != null && this.streamWriter.BaseStream.CanWrite) {
				this.WriteHeader ();
				this.streamWriter.Close ();
				this.streamWriter = null;
			}

			this.stream = null;
		}


		public void Dispose ()
		{
			StopRecorder ();
		}


		void WriteHeader ()
		{
			this.writer.Seek (0, SeekOrigin.Begin);
			// chunk ID
			this.writer.Write ('R');
			this.writer.Write ('I');
			this.writer.Write ('F');
			this.writer.Write ('F');

			this.writer.Write (this.byteCount + 36);
			this.writer.Write ('W');
			this.writer.Write ('A');
			this.writer.Write ('V');
			this.writer.Write ('E');

			this.writer.Write ('f');
			this.writer.Write ('m');
			this.writer.Write ('t');
			this.writer.Write (' ');

			this.writer.Write (16);
			this.writer.Write ((short)1);

			this.writer.Write ((short)this.stream.ChannelCount);
			this.writer.Write (this.stream.SampleRate);
			this.writer.Write (this.stream.SampleRate * 2);
			this.writer.Write ((short)2);
			this.writer.Write ((short)this.stream.BitsPerSample);
			this.writer.Write ('d');
			this.writer.Write ('a');
			this.writer.Write ('t');
			this.writer.Write ('a');
			this.writer.Write (this.byteCount);
		}
	}
}