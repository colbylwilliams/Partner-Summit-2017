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
using AudioToolbox;

namespace XWeather.WeatherBot
{
	public class AudioStream
	{
		InputAudioQueue audioQueue;

		readonly int bufferSize;

		public event EventHandler<byte []> OnBroadcast;

		public event EventHandler<bool> OnActiveChanged;

		public event EventHandler<Exception> OnException;

		public int SampleRate {
			get;
			private set;
		}


		public int ChannelCount {
			get {
				return 1;
			}
		}


		public int BitsPerSample {
			get {
				return 16;
			}
		}


		public bool Start ()
		{
			var success = this.audioQueue.Start () == AudioQueueStatus.Ok;

			if (success) {
				OnActiveChanged?.Invoke (this, true);
			}

			return success;
		}


		public void Stop ()
		{
			this.audioQueue.Stop (true);
			OnActiveChanged?.Invoke (this, false);
		}


		public bool Active {
			get {
				return this.audioQueue.IsRunning;
			}
		}


		public AudioStream (int sampleRate, int bufferSize)
		{
			this.SampleRate = sampleRate;
			this.bufferSize = bufferSize;
			this.Init ();
		}


		void Init ()
		{
			var audioFormat = new AudioStreamBasicDescription {
				SampleRate = this.SampleRate,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
				FramesPerPacket = 1,
				ChannelsPerFrame = 1,
				BitsPerChannel = this.BitsPerSample,
				BytesPerPacket = 2,
				BytesPerFrame = 2,
				Reserved = 0
			};

			audioQueue = new InputAudioQueue (audioFormat);
			audioQueue.InputCompleted += QueueInputCompleted;

			var bufferByteSize = this.bufferSize * audioFormat.BytesPerPacket;

			IntPtr bufferPtr;

			for (var index = 0; index < 3; index++) {
				audioQueue.AllocateBufferWithPacketDescriptors (bufferByteSize, this.bufferSize, out bufferPtr);
				audioQueue.EnqueueBuffer (bufferPtr, bufferByteSize, null);
			}
		}


		/// <summary>
		/// Handles iOS audio buffer queue completed message.
		/// </summary>
		/// <param name='sender'>Sender object</param>
		/// <param name='e'> Input completed parameters.</param>
		void QueueInputCompleted (object sender, InputCompletedEventArgs e)
		{
			// return if we aren't actively monitoring audio packets
			if (!this.Active) {
				return;
			}

			var buffer = (AudioQueueBuffer)System.Runtime.InteropServices.Marshal.PtrToStructure (e.IntPtrBuffer, typeof (AudioQueueBuffer));

			var send = new byte [buffer.AudioDataByteSize];
			System.Runtime.InteropServices.Marshal.Copy (buffer.AudioData, send, 0, (int)buffer.AudioDataByteSize);

			this.OnBroadcast?.Invoke (this, send);

			var status = audioQueue.EnqueueBuffer (e.IntPtrBuffer, this.bufferSize, e.PacketDescriptions);

			if (status != AudioQueueStatus.Ok) {
				OnException?.Invoke (this, new Exception ($"audioQueue.EnqueueBuffer returned non-Ok status :: {status}"));
			}
		}
	}
}