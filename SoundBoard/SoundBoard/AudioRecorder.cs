﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Diagnostics;

namespace SoundBoard
{
    class AudioRecorder
    {
        public WasapiLoopbackCapture MyWaveIn;
        public readonly double RecordTime;

        private WaveOutEvent _wav = new WaveOutEvent();
        private bool _isFull = false;
        private int _pos = 0;
        private byte[] _buffer;
        private bool _isRecording = false;

        /// <summary>
        /// Creates a new recorder with a buffer
        /// </summary>
        /// <param name="recordTime">Time to keep in the buffer (in seconds)</param>
        public AudioRecorder(double recordTime)
        {
            RecordTime = recordTime;
            MyWaveIn = new WasapiLoopbackCapture();
            MyWaveIn.DataAvailable += DataAvailable;
            _buffer = new byte[(int)(MyWaveIn.WaveFormat.AverageBytesPerSecond * RecordTime)];
        }

        /// <summary>
        /// Starts recording
        /// </summary>
        public void StartRecording()
        {
            if (!_isRecording)
            {
                try
                {
                    MyWaveIn.StartRecording();
                    Debug.WriteLine("Started Recoring!");
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("Already Recording!");
                }
            }

            _isRecording = true;
        }

        /// <summary>
        /// Stops recording
        /// </summary>
        public void StopRecording()
        {
            MyWaveIn.StopRecording();
            _isRecording = false;
            Debug.WriteLine("Stopped Recording!");
        }

        /// <summary>
        /// Plays currently recorded data
        /// </summary>
        public void PlayRecorded()
        {
            if(_wav.PlaybackState == PlaybackState.Stopped)
            {
                var buff = new BufferedWaveProvider(MyWaveIn.WaveFormat);
                var bytes = GetBytesToSave();
                buff.AddSamples(bytes, 0, bytes.Length);
                _wav.Init(buff);
                _wav.Play();
            }
        }

        /// <summary>
        /// Stops replay
        /// </summary>
        public void StopReplay()
        {
            if (_wav != null) _wav.Stop();
        }

        /// <summary>
        /// Save to disk
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            var writer = new WaveFileWriter(fileName, MyWaveIn.WaveFormat);
            var buff = GetBytesToSave();
            writer.Write(buff, 0, buff.Length);
            writer.Flush();
            writer.Dispose();
            Debug.WriteLine("File Saved!");
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {
            for (int i = 0; i < e.BytesRecorded; i++)
            {
                // save the data
                _buffer[_pos] = e.Buffer[i];
                // move the current position (advances by 1 OR resets to zero if the length of the buffer was reached)
                _pos = (_pos + 1) % _buffer.Length;
                // flag if the buffer is full (will only set it from false to true the first time it reaches the full length of the buffer)
                _isFull |= (_pos == 0);
            }
        }

        private byte[] GetBytesToSave()
        {
            int length = _isFull ? _buffer.Length : _pos;
            var bytesToSave = new byte[length];
            int byteCountToEnd = _isFull ? (_buffer.Length - _pos) : 0;
            if (byteCountToEnd > 0)
            {
                // bytes from the current position to the end
                Array.Copy(_buffer, _pos, bytesToSave, 0, byteCountToEnd);
            }
            if (_pos > 0)
            {
                // bytes from the start to the current position
                Array.Copy(_buffer, 0, bytesToSave, byteCountToEnd, _pos);
            }
            return bytesToSave;
        }

        /// <summary>
        /// Starts Recording if WaveIn stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Stopped(object sender, StoppedEventArgs e)
        {
            Debug.WriteLine("Recording stopped!");
            if (e.Exception != null) Debug.WriteLine(e.Exception.Message);
            if (_isRecording)
            {
                MyWaveIn.StartRecording();
            }
        }
    }
}