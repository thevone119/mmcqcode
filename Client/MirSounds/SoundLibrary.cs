﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DirectX.DirectSound;

namespace Client.MirSounds
{

    class SoundLibrary : IDisposable
    {
        public int Index;

        private List<SecondaryBuffer> _bufferList;

        private MemoryStream _mStream;
        private bool _loop;
        //加多一个文件名的参数，方便调试
        public string fname;

        public SoundLibrary(int index, string fileName, bool loop)
        {
            Index = index;
            fname = fileName;
            fileName = Path.Combine(Settings.SoundPath, fileName);
            if (!File.Exists(fileName)) return;

            _mStream = new MemoryStream(File.ReadAllBytes(fileName));

            _loop = loop;

            _bufferList = new List<SecondaryBuffer>();

            Play();
        }

        public void Play()
        {
            //这里经常有问题，关闭声音的话，直接返回吧
            if (Settings.Volume<5)
            {
                return;
            }
            if (_mStream == null|| _mStream.Length<10) return;

            _mStream.Seek(0, SeekOrigin.Begin);
            //声音播放直接捕获异常，声音播放不了，也不出现问题
            try
            {
                if (_loop)
                {
                    if (_bufferList.Count == 0)
                        _bufferList.Add(new SecondaryBuffer(_mStream, new BufferDescription { BufferBytes = (int)_mStream.Length, ControlVolume = true }, SoundManager.Device) { Volume = SoundManager.Vol });
                    else if (_bufferList[0] == null || _bufferList[0].Disposed)
                        _bufferList[0] = new SecondaryBuffer(_mStream, new BufferDescription { BufferBytes = (int)_mStream.Length, ControlVolume = true }, SoundManager.Device) { Volume = SoundManager.Vol };

                    if (!_bufferList[0].Status.Playing)
                        _bufferList[0].Play(0, BufferPlayFlags.Looping);
                }
                else
                {
                    for (int i = _bufferList.Count - 1; i >= 0; i--)
                    {
                        if (_bufferList[i] == null || _bufferList[i].Disposed)
                        {
                            _bufferList.RemoveAt(i);
                            continue;
                        }

                        if (!_bufferList[i].Status.Playing)
                        {
                            _bufferList[i].Play(0, BufferPlayFlags.Default);
                            return;
                        }
                    }

                    if (_bufferList.Count >= Settings.SoundOverLap) return;

                    SecondaryBuffer buffer = new SecondaryBuffer(_mStream, new BufferDescription { BufferBytes = (int)_mStream.Length, ControlVolume = true }, SoundManager.Device) { Volume = SoundManager.Vol };
                    buffer.Play(0, BufferPlayFlags.Default);
                    _bufferList.Add(buffer);
                }
            }
            catch(Exception e)
            {
                CMain.SaveError(e.ToString());
                CMain.SaveError("error:"+fname);
                
            }
        }
        public void Stop()
        {
            if (_bufferList == null || _bufferList.Count == 0) return;

            if (_loop)
                _bufferList[0].Dispose();
            else
            {
                for (int i = 0; i < _bufferList.Count; i++)
                    _bufferList[i].Dispose();
                _bufferList.Clear();
            }
        }

        public void Dispose()
        {
            if (_mStream != null)
                _mStream.Dispose();
            _mStream = null;

            if (_bufferList != null)
                for (int i = 0; i < _bufferList.Count; i++)
                    _bufferList[i].Dispose();
            _bufferList = null;

            _loop = false;
        }

        public void SetVolume(int vol)
        {
            if (vol <= -3000) vol = -10000;

            for (int i = 0; i < _bufferList.Count; i++)
                if (_bufferList[i] != null && !_bufferList[i].Disposed)
                {
                    _bufferList[i].Volume = vol;
                }
        }
    }
}
