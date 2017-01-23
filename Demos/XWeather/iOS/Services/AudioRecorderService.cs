using System;
using System.IO;

namespace XWeather.WeatherBot
{
	public class AudioRecorderService
	{
		const float AUDIOMONITOR_THRESHOLD = .15f;

		static float MAX_8_BITS_SIGNED = byte.MaxValue;
		static float MAX_8_BITS_UNSIGNED = 0xff;
		static float MAX_16_BITS_SIGNED = short.MaxValue;
		static float MAX_16_BITS_UNSIGNED = 0xffff;

		AudioStream audioStream;
		WaveRecorder recorder;

		bool endOnSilence = true;
		bool audioDetected;
		DateTime? silenceTime;
		TimeSpan audioTimeout = TimeSpan.FromSeconds (2);

		public event EventHandler<string> AudioInputReceived;


		public void StartRecording ()
		{
			audioDetected = false;
			silenceTime = null;

			InitializeStream ();

			if (!recorder.StartRecorder (audioStream, GetFilename ())) {
				throw new Exception ("AudioStream failed to start: busy?");
			}
		}


		void AudioStream_OnBroadcast (object sender, byte [] bytes)
		{
			var level = calculateLevel (bytes, 0, 0);

			//System.Diagnostics.Debug.WriteLine ("AudioStream_OnBroadcast :: calculateLevel == {0}", level);

			if (level > .20) //did we find a signal?
			{
				audioDetected = true;
				silenceTime = null;
			} else //no audio detected
			  {
				//see if we've detected 'near' silence for more than <audioTimeout>
				if (endOnSilence && silenceTime.HasValue) {
					if (DateTime.Now.Subtract (silenceTime.Value) > audioTimeout) {
						StopRecording ();
					}
				} else {
					silenceTime = DateTime.Now;
				}
			}
		}


		// Adapted from http://stackoverflow.com/questions/5800649/detect-silence-when-recording
		float calculateLevel (byte [] buffer, int readPoint, int leftOver, bool use16Bit = true)
		{
			float level;
			int max = 0;
			//bool use16Bit = (RECORDER_BPP == 16);
			bool signed = true;
			//bool signed = (RECORDER_AUDIO_ENCODING == Android.Media.Encoding. AudioFormat. Encoding.PCM_SIGNED);
			bool bigEndian = false;// (format.isBigEndian());

			if (use16Bit) {
				for (int i = readPoint; i < buffer.Length - leftOver; i += 2) {
					int value = 0;
					// deal with endianness
					int hiByte = (bigEndian ? buffer [i] : buffer [i + 1]);
					int loByte = (bigEndian ? buffer [i + 1] : buffer [i]);

					if (signed) {
						short shortVal = (short)hiByte;
						shortVal = (short)((shortVal << 8) | (byte)loByte);
						value = shortVal;
					} else {
						value = (hiByte << 8) | loByte;
					}

					max = Math.Max (max, value);
				} // for
			} else {
				// 8 bit - no endianness issues, just sign
				for (int i = readPoint; i < buffer.Length - leftOver; i++) {
					int value = 0;

					if (signed) {
						value = buffer [i];
					} else {
						short shortVal = 0;
						shortVal = (short)(shortVal | buffer [i]);
						value = shortVal;
					}

					max = Math.Max (max, value);
				} // for
			} // 8 bit
			  // express max as float of 0.0 to 1.0 of max value
			  // of 8 or 16 bits (signed or unsigned)
			if (signed) {
				if (use16Bit) { level = (float)max / MAX_16_BITS_SIGNED; } else { level = (float)max / MAX_8_BITS_SIGNED; }
			} else {
				if (use16Bit) { level = (float)max / MAX_16_BITS_UNSIGNED; } else { level = (float)max / MAX_8_BITS_UNSIGNED; }
			}

			//System.Console.WriteLine ("LEVEL is {0}", level);

			return level;
		}


		public void StopRecording ()
		{
			if (audioStream == null)
				throw new Exception ("You must first start recording.");

			if (audioStream.Active) {
				try {

					audioStream.OnBroadcast -= AudioStream_OnBroadcast;
					recorder.StopRecorder ();
					audioStream.Stop ();

				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex.Message);
				}

				AudioInputReceived?.Invoke (this, audioDetected ? GetFilename () : null);
			}
		}


		void InitializeStream ()
		{
			try {
				if (audioStream == null) {
					audioStream = new AudioStream (44100, 640);
				}

				if (endOnSilence) {
					audioStream.OnBroadcast -= AudioStream_OnBroadcast;
					audioStream.OnBroadcast += AudioStream_OnBroadcast;
				}

				if (recorder == null) {
					recorder = new WaveRecorder ();
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine ("Error: {0}", ex);
			}
		}


		string GetFilename ()
		{
			return Path.Combine (Path.GetTempPath (), "recording.wav");
		}
	}
}