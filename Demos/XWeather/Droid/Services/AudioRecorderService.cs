using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Java.IO;


// Ported from: http://www.edumobile.org/android/audio-recording-in-wav-format-in-android-programming/
namespace XWeather.WeatherBot
{
	public class AudioRecorderService
	{
		static float MAX_8_BITS_SIGNED = byte.MaxValue;
		static float MAX_8_BITS_UNSIGNED = 0xff;
		static float MAX_16_BITS_SIGNED = short.MaxValue;
		static float MAX_16_BITS_UNSIGNED = 0xffff;

		int RECORDER_BPP = 16;
		int RECORDER_SAMPLERATE = 44100;
		ChannelIn RECORDER_CHANNELS = ChannelIn.Stereo;
		Encoding RECORDER_AUDIO_ENCODING = Encoding.Pcm16bit;
		TimeSpan audioTimeout = TimeSpan.FromSeconds (2);

		AudioRecord recorder;
		int bufferSize;
		bool isRecording;
		bool endOnSilence = true;

		public event EventHandler<string> AudioInputReceived;

		public void Init ()
		{
			bufferSize = AudioRecord.GetMinBufferSize (RECORDER_SAMPLERATE, RECORDER_CHANNELS, Encoding.Pcm16bit);
		}


		public void StartRecording ()
		{
			Init ();

			var context = Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity;
			var audioManager = (AudioManager)context.GetSystemService (Context.AudioService);
			RECORDER_SAMPLERATE = int.Parse (audioManager.GetProperty (AudioManager.PropertyOutputSampleRate));

			if (recorder != null)
				recorder.Release ();

			// Calculate buffer size
			//bufferSize = AudioRecord.GetMinBufferSize(RECORDER_SAMPLERATE, ChannelIn.Mono, Encoding.Pcm16bit);
			recorder = new AudioRecord (AudioSource.Mic, RECORDER_SAMPLERATE, RECORDER_CHANNELS, RECORDER_AUDIO_ENCODING, bufferSize);

			recorder.StartRecording ();

			isRecording = true;

			var token = new CancellationTokenSource ();
			Task.Run (() => WriteAudioDataToFile (), token.Token);
		}


		void WriteAudioDataToFile ()
		{
			bool audioDetected = false;
			byte [] data = new byte [bufferSize];
			var filename = GetTempFilename ();
			FileOutputStream os = null;

			System.Diagnostics.Debug.WriteLine (filename);

			os = new FileOutputStream (filename);

			int readResult = 0;
			DateTime? silenceTime = null;

			if (os != null)
			{
				while (isRecording)
				{
					readResult = recorder.Read (data, 0, bufferSize);

					if (readResult > 0)// Xamarin seems to have normalized this buffer read like most .NET reads, readResult == the # bytes read, so we can't compare to TrackStatus.Success, etc.
					{
						var level = calculateLevel (data, 0, 0);

						if (level > .25) //foundAudio) //did we find a signal?
						{
							audioDetected = true;
							silenceTime = null;
						}
						else //no audio detected
						{
							//see if we've detected 'near' silence for more than <audioTimeout>
							if (endOnSilence && silenceTime.HasValue)
							{
								if (DateTime.Now.Subtract (silenceTime.Value) > audioTimeout)
								{
									StopRecording ();
								}
							}
							else
							{
								silenceTime = DateTime.Now;
							}
						}

						//write the data to disk
						try
						{
							os.Write (data);
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine (ex.Message);
						}
					}
				}

				try
				{
					os.Close ();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine (ex.Message);
				}

				AudioInputReceived?.Invoke (this, audioDetected ? GetFilename () : null);
			}
		}

		/// <summary>
		/// Stops the recording.
		/// </summary>
		public void StopRecording ()
		{
			if (recorder != null)
			{
				isRecording = false;

				recorder.Stop ();
				// token.Cancel();

				recorder.Release ();
				recorder = null;
			}

			CopyWaveFile (GetTempFilename (), GetFilename ());
		}


		// Adapted from http://stackoverflow.com/questions/5800649/detect-silence-when-recording
		float calculateLevel (byte [] buffer, int readPoint, int leftOver)
		{
			float level;
			int max = 0;
			bool use16Bit = (RECORDER_BPP == 16);
			bool signed = true;
			//bool signed = (RECORDER_AUDIO_ENCODING == Android.Media.Encoding. AudioFormat. Encoding.PCM_SIGNED);
			bool bigEndian = false;// (format.isBigEndian());

			if (use16Bit)
			{
				for (int i = readPoint; i < buffer.Length - leftOver; i += 2)
				{
					int value = 0;
					// deal with endianness
					int hiByte = (bigEndian ? buffer [i] : buffer [i + 1]);
					int loByte = (bigEndian ? buffer [i + 1] : buffer [i]);

					if (signed)
					{
						short shortVal = (short)hiByte;
						shortVal = (short)((shortVal << 8) | (byte)loByte);
						value = shortVal;
					}
					else
					{
						value = (hiByte << 8) | loByte;
					}
					max = Math.Max (max, value);
				} // for
			}
			else
			{
				// 8 bit - no endianness issues, just sign
				for (int i = readPoint; i < buffer.Length - leftOver; i++)
				{
					int value = 0;

					if (signed)
					{
						value = buffer [i];
					}
					else
					{
						short shortVal = 0;
						shortVal = (short)(shortVal | buffer [i]);
						value = shortVal;
					}

					max = Math.Max (max, value);
				} // for
			} // 8 bit
			  // express max as float of 0.0 to 1.0 of max value
			  // of 8 or 16 bits (signed or unsigned)
			if (signed)
			{
				if (use16Bit) { level = (float)max / MAX_16_BITS_SIGNED; } else { level = (float)max / MAX_8_BITS_SIGNED; }
			}
			else
			{
				if (use16Bit) { level = (float)max / MAX_16_BITS_UNSIGNED; } else { level = (float)max / MAX_8_BITS_UNSIGNED; }
			}

			//System.Console.WriteLine ("LEVEL is {0}", level);

			return level;
		}


		string GetFilename ()
		{
			var path = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
			return System.IO.Path.Combine (path, "recording.wav");
		}


		string GetTempFilename ()
		{
			var path = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
			return System.IO.Path.Combine (path, "temp.raw");
		}


		void DeleteTempFile ()
		{
			var file = new File (GetTempFilename ());
			file.Delete ();
		}


		void CopyWaveFile (string tempFile, string permanentFile)
		{
			FileInputStream input = null;
			FileOutputStream output = null;
			long totalAudioLen = 0;
			long totalDataLen = totalAudioLen + 36;
			long longSampleRate = RECORDER_SAMPLERATE;
			int channels = 2;
			long byteRate = RECORDER_BPP * RECORDER_SAMPLERATE * channels / 8;

			byte [] data = new byte [bufferSize];

			try
			{
				input = new FileInputStream (tempFile);
				output = new FileOutputStream (permanentFile);
				totalAudioLen = input.Channel.Size ();
				totalDataLen = totalAudioLen + 36;

				System.Diagnostics.Debug.WriteLine ($"File Size: {totalDataLen}");

				WriteWaveFileHeader (output, totalAudioLen, totalDataLen, longSampleRate, channels, byteRate);

				while (input.Read (data) != -1)
				{
					output.Write (data);
				}

				input.Close ();
				output.Close ();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine (ex.Message);
			}
		}


		void WriteWaveFileHeader (FileOutputStream output, long totalAudioLen, long totalDataLen, long longSampleRate,
								 int channels, long byteRate)
		{
			byte [] header = new byte [44];

			header [0] = Convert.ToByte ('R'); // RIFF/WAVE header
			header [1] = Convert.ToByte ('I');//  (byte)'I';
			header [2] = Convert.ToByte ('F');
			header [3] = Convert.ToByte ('F');
			header [4] = (byte)(totalDataLen & 0xff);
			header [5] = (byte)((totalDataLen >> 8) & 0xff);
			header [6] = (byte)((totalDataLen >> 16) & 0xff);
			header [7] = (byte)((totalDataLen >> 24) & 0xff);
			header [8] = Convert.ToByte ('W');
			header [9] = Convert.ToByte ('A');
			header [10] = Convert.ToByte ('V');
			header [11] = Convert.ToByte ('E');
			header [12] = Convert.ToByte ('f');// 'fmt ' chunk
			header [13] = Convert.ToByte ('m');
			header [14] = Convert.ToByte ('t');
			header [15] = (byte)' ';
			header [16] = 16; // 4 bytes: size of 'fmt ' chunk
			header [17] = 0;
			header [18] = 0;
			header [19] = 0;
			header [20] = 1; // format = 1
			header [21] = 0;
			header [22] = Convert.ToByte (channels);
			header [23] = 0;
			header [24] = (byte)(longSampleRate & 0xff);
			header [25] = (byte)((longSampleRate >> 8) & 0xff);
			header [26] = (byte)((longSampleRate >> 16) & 0xff);
			header [27] = (byte)((longSampleRate >> 24) & 0xff);
			header [28] = (byte)(byteRate & 0xff);
			header [29] = (byte)((byteRate >> 8) & 0xff);
			header [30] = (byte)((byteRate >> 16) & 0xff);
			header [31] = (byte)((byteRate >> 24) & 0xff);
			header [32] = (byte)(2 * 16 / 8); // block align
			header [33] = 0;
			header [34] = Convert.ToByte (RECORDER_BPP); // bits per sample
			header [35] = 0;
			header [36] = Convert.ToByte ('d');
			header [37] = Convert.ToByte ('a');
			header [38] = Convert.ToByte ('t');
			header [39] = Convert.ToByte ('a');
			header [40] = (byte)(totalAudioLen & 0xff);
			header [41] = (byte)((totalAudioLen >> 8) & 0xff);
			header [42] = (byte)((totalAudioLen >> 16) & 0xff);
			header [43] = (byte)((totalAudioLen >> 24) & 0xff);

			output.Write (header, 0, 44);
		}
	}
}